using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Models;

namespace Karmobil.Api.Endpoints;

public static class AracEndpoints
{
    public static void MapArac(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api").WithTags("Araçlar").RequireAuthorization();

        // Haritadaki araçlar ve listesi; arayüz bunu birkaç saniyede bir yeniden çeker.
        grup.MapGet("/araclar", async (int? grupId, HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();

            var satirlar = await db.SorguAsync(
                Sql.AracKonumlari,
                new Dictionary<string, object?> { ["kullaniciId"] = k.Id, ["grupId"] = grupId },
                ct);

            return Results.Ok(satirlar.Select(AracDto).ToList());
        });

        grup.MapGet("/gruplar", async (HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();

            var satirlar = await db.SorguAsync(Sql.Gruplar, new Dictionary<string, object?> { ["kullaniciId"] = k.Id }, ct);
            return Results.Ok(satirlar.Select(s => new GrupDto(
                Tam(s["ID"]), (s["GROUPNAME"] as string ?? "").Trim(), s["PARENTID"] is null ? null : Tam(s["PARENTID"]))).ToList());
        });

        grup.MapGet("/mesajlar/okunmamis", async (HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            var sayi = await db.TekDegerAsync<int>(Sql.OkunmamisMesajSayisi, new Dictionary<string, object?> { ["kullaniciId"] = k.Id.ToString() }, ct);
            return Results.Ok(new { sayi });
        });
    }

    private static AracKonumDto AracDto(Dictionary<string, object?> s)
    {
        var dvr = (s["DVRIPNO"] as string)?.Trim();
        return new AracKonumDto(
            Id: Tam(s["ID"]),
            CihazId: (s["CIHAZID"] as string ?? "").Trim(),
            Plaka: (s["PLAKA"] as string ?? "").Trim(),
            Durum: (string)s["DURUM"]!,
            Kontak: (s["KONTAK"]?.ToString() ?? "0").Trim() == "1",
            Lat: Ondalik(s["LAT"]),
            Lon: Ondalik(s["LON"]),
            Hiz: Ondalik(s["HIZ"]) ?? 0,
            Yon: Ondalik(s["YON"]) ?? 0,
            GpsHatasi: (s["GPSHATA"]?.ToString() ?? "0").Trim() != "0",
            Batarya: s["BATARYA"]?.ToString()?.Trim(),
            Isi: s["ISI"]?.ToString()?.Trim(),
            Surucu: BosIseNull(s["SURUCU"] as string),
            GpsTarihi: s["GPS_TARIH"] as DateTime?,
            SinyalTarihi: s["SINYAL_TARIH"] as DateTime?,
            Adres: MetinTemizle(BosIseNull(s["ADRES"] as string)),
            CihazTipi: BosIseNull(s["CIHAZTIPI"] as string),
            GrupIdleri: GrupListesi(s["FILO_IDS"] as string),
            Dvr: !string.IsNullOrEmpty(dvr) && dvr != "1.1.1.1",
            MotorBlokeli: (s["MOTOR"]?.ToString() ?? "0").Trim() == "1",
            GunlukKm: Ondalik(s["GUNLUK_KM"]),
            KadranKm: Ondalik(s["KADRAN_KM"]));
    }

    private static int Tam(object? v) => Convert.ToInt32(v, CultureInfo.InvariantCulture);

    private static double? Ondalik(object? v)
    {
        if (v is null) return null;
        if (v is string str)
            return double.TryParse(str.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
        return Convert.ToDouble(v, CultureInfo.InvariantCulture);
    }

    private static string? BosIseNull(string? v)
    {
        var t = v?.Trim();
        return string.IsNullOrEmpty(t) || t == "-" ? null : t;
    }

    // SKKONUM'da yanlış kodlamadan kalma C1 kontrol karakterleri var (ör. "Ş" + U+009E → "Ş☐ehitkamil").
    private static string? MetinTemizle(string? v) =>
        v is null ? null : System.Text.RegularExpressions.Regex.Replace(v.Normalize(), "[\u0080-\u009F​-‏﻿]", "");

    private static List<int> GrupListesi(string? v) =>
        (v ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
            .Where(x => x.HasValue).Select(x => x!.Value).ToList();
}
