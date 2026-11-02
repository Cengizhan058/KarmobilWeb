using System.Globalization;
using System.Text.RegularExpressions;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Models;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Geçmiş izleme (eski: reports005g9.js report_past + data/_raporKonum.asp).
/// Saklı yordam aynen kullanılır; satır yorumlama kuralları _raporKonum.asp'den taşındı.
/// "Park halindeki konumları grupla" artık arayüzde yapılır; API duraklamaları ayrıca döner.
/// </summary>
public static class GecmisEndpoints
{
    // Eski form: tarih aralığı 1 aydan fazla olamaz (fark > 32 gün hata).
    private const int AzamiGun = 32;
    private static readonly Regex KontrolKarakteri = new("[\u0080-\u009F​-‏﻿]", RegexOptions.Compiled);

    public static void MapGecmis(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/gecmis", async (
            int aracId, DateTime baslangic, DateTime bitis, bool? poi,
            HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            if (!k.Raporlar) return Results.Json(new { error = "Rapor yetkiniz yok." }, statusCode: StatusCodes.Status403Forbidden);

            if (bitis <= baslangic) return Results.BadRequest(new { error = "Bitiş, başlangıçtan sonra olmalı." });
            if ((bitis - baslangic).TotalDays > AzamiGun) return Results.BadRequest(new { error = "Tarih aralığı 1 aydan fazla olamaz." });

            // Yordam aracın kullanıcıya ait olduğunu denetlemiyor; burada denetlenir.
            var arac = await db.TekSatirAsync(Sql.AracDetay, new Dictionary<string, object?> { ["aracId"] = aracId, ["kullaniciId"] = k.Id }, ct);
            if (arac is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            var cihazId = (arac["CIHAZID"] as string ?? "").Trim();

            var p = new Dictionary<string, object?>
            {
                ["cihazId"] = cihazId,
                ["t1"] = baslangic.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["t2"] = bitis.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["kullaniciId"] = k.Id,
            };

            var satirlar = await db.SorguAsync(poi == true ? Sql.GecmisKonumlarPoi : Sql.GecmisKonumlar, p, ct);
            var mesafe = await db.TekDegerAsync<object>(Sql.AralikMesafe, p, ct);

            var noktalar = new List<GecmisNoktaDto>(satirlar.Count);
            var suruculer = new Dictionary<string, string?>();
            string? surucu = null;

            foreach (var s in satirlar)
            {
                var kod = (s["RAPORKODU"] as string ?? "").Trim();
                if (kod == "VI")
                {
                    var kart = (s["SITUATIONS"] as string ?? "").Trim();
                    if (!suruculer.TryGetValue(kart, out surucu))
                    {
                        surucu = Bos(await db.TekDegerAsync<string>(Sql.SurucuBul, new Dictionary<string, object?> { ["kullaniciId"] = k.Id.ToString(), ["kart"] = kart }, ct));
                        suruculer[kart] = surucu;
                    }
                }
                if (kod == "G1") surucu = null;

                var hiz = Convert.ToInt32(s["HIZ"] ?? 0, CultureInfo.InvariantCulture);
                if (hiz < 6) hiz = 0;

                noktalar.Add(new GecmisNoktaDto(
                    Id: Convert.ToInt64(s["ID"], CultureInfo.InvariantCulture),
                    Zaman: (DateTime)s["TARIH"]!,
                    Lat: Convert.ToDouble(s["Y"], CultureInfo.InvariantCulture),
                    Lon: Convert.ToDouble(s["X"], CultureInfo.InvariantCulture),
                    Hiz: hiz,
                    Yon: s["YON"] is null ? 0 : Convert.ToDouble(s["YON"], CultureInfo.InvariantCulture),
                    Kontak: Convert.ToInt32(s["KONTAK"] ?? 0, CultureInfo.InvariantCulture) == 1,
                    RaporKodu: kod,
                    Durum: DurumMetni(kod, hiz),
                    Alarm: Bos(s["alarm"] as string),
                    Adres: Temizle(Bos(s["konumx"] as string)),
                    Surucu: surucu,
                    Isi: s["ISI"] is null ? null : Convert.ToDouble(s["ISI"], CultureInfo.InvariantCulture),
                    KadranKm: s["kadranm"] is null ? null : Convert.ToDouble(s["kadranm"], CultureInfo.InvariantCulture)));
            }

            return Results.Ok(new GecmisDto(
                AracId: aracId,
                Plaka: (arac["PLAKA"] as string ?? "").Trim(),
                Baslangic: baslangic,
                Bitis: bitis,
                ToplamKm: mesafe is null ? 0 : Math.Round(Convert.ToDouble(mesafe, CultureInfo.InvariantCulture) / 1000, 2),
                AzamiHiz: noktalar.Count == 0 ? 0 : noktalar.Max(n => n.Hiz),
                Noktalar: noktalar,
                Duraklar: Duraklar(noktalar)));
        })
        .WithTags("Geçmiş")
        .RequireAuthorization();
    }

    // _raporKonum.asp d_txt kuralı.
    private static string DurumMetni(string kod, int hiz) => kod switch
    {
        "AH" => "Hız alarmı",
        "G2" => "Kontak açıldı",
        "G1" => "Kontak kapandı",
        "VK" => "Anlık konum isteği",
        "AA" => "Panik alarmı",
        "XI" => "Isı alarmı",
        "XR" => "Rölanti alarmı",
        _ => hiz == 0 ? "Park halinde" : "Hareket halinde",
    };

    // Ardışık hız=0 kayıtları bir duraklama; eski "X dakika durdu" satırının karşılığı.
    private static List<DuraklamaDto> Duraklar(List<GecmisNoktaDto> noktalar)
    {
        var sonuc = new List<DuraklamaDto>();
        var i = 0;
        while (i < noktalar.Count)
        {
            if (noktalar[i].Hiz != 0) { i++; continue; }
            var bas = i;
            while (i + 1 < noktalar.Count && noktalar[i + 1].Hiz == 0) i++;
            // Duraklama, sonraki hareket kaydına kadar sürer (yoksa son kayda kadar).
            var bitisZamani = i + 1 < noktalar.Count ? noktalar[i + 1].Zaman : noktalar[i].Zaman;
            var dakika = (int)Math.Max(1, Math.Round((bitisZamani - noktalar[bas].Zaman).TotalMinutes));
            var n = noktalar[bas];
            sonuc.Add(new DuraklamaDto(n.Zaman, bitisZamani, dakika, n.Lat, n.Lon, n.Adres, n.Kontak));
            i++;
        }
        return sonuc;
    }

    private static string? Bos(string? v)
    {
        var t = v?.Trim();
        return string.IsNullOrEmpty(t) || t == "-" || t.Equals("<null>", StringComparison.OrdinalIgnoreCase) ? null : t;
    }

    private static string? Temizle(string? v) => v is null ? null : KontrolKarakteri.Replace(v.Normalize(), "");
}
