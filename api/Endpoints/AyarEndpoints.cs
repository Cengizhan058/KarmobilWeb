using System.Text.RegularExpressions;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Ruhsat bilgileri (eski data/_ruhsatBilgileri.asp), mesai saatleri (_getWorkHours / _prcWorkHours) ve
/// RF-ID sürücü kartları (_getRfIdCards / _prcRFID).
/// Eski ruhsat ucunda sahiplik denetimi yoktu (her aracın ruhsatı okunup yazılabiliyordu); burada userclients ile sınırlı.
/// </summary>
public static partial class AyarEndpoints
{
    /// <summary>Ruhsat alanları: kolon, başlık (ruhsattaki kod), azami uzunluk (clients tablosundaki nvarchar boyu).</summary>
    public static readonly (string Kolon, string Baslik, int Uz)[] RuhsatAlanlari =
    [
        ("RUHSAT_PLAKA", "(A) Plaka", 50),
        ("RUHSAT_ILKTESCILTARIH", "(B) İlk tescil tarihi", 50),
        ("RUHSAT_TESCILSIRANO", "(Y.2) Tescil sıra no", 50),
        ("RUHSAT_TESCILTARIH", "(I) Tescil tarihi", 50),
        ("RUHSAT_VERILDIGIILILCE", "(Y.1) Verildiği il/ilçe", 50),
        ("RUHSAT_MARKA", "(D.1) Markası", 50),
        ("RUHSAT_TIPI", "(D.2) Tipi", 50),
        ("RUHSAT_TICARIADI", "(D.3) Ticari adı", 50),
        ("RUHSAT_MODELYILI", "(D.4) Model yılı", 50),
        ("RUHSAT_CINSI", "(D.5) Cinsi", 50),
        ("RUHSAT_ARACSINIFI", "(J) Araç sınıfı", 50),
        ("RUHSAT_RENGI", "(R) Rengi", 50),
        ("RUHSAT_MOTORNO", "(P.5) Motor no", 50),
        ("RUHSAT_SASENO", "(E) Şase no", 50),
        ("RUHSAT_NETAGIRLIK", "(G.1) Net ağırlığı", 10),
        ("RUHSAT_AZAMIYUK", "(F.1) Azami yüklü ağırlığı", 10),
        ("RUHSAT_KATARAGIRLIK", "(G) Katar ağırlığı", 10),
        ("RUHSAT_RAMORKYUKLU", "(G.2) Römork azami yüklü ağırlığı", 10),
        ("RUHSAT_KOLTUKSAYISI", "(S.1) Koltuk sayısı (sürücü dahil)", 10),
        ("RUHSAT_YOLCUSAYISI", "(S.2) Ayakta yolcu sayısı", 10),
        ("RUHSAT_SILINDIRHACMI", "(P.1) Silindir hacmi", 10),
        ("RUHSAT_MOTORGUCU", "(P.2) Motor gücü", 10),
        ("RUHSAT_YAKITCINSI", "(P.3) Yakıt cinsi", 20),
        ("RUHSAT_GUCAGIRLIGI", "(Q) Güç/ağırlık oranı (motosiklet)", 20),
        ("RUHSAT_KULLANIMAMACI", "(Y.3) Kullanım amacı", 50),
        ("RUHSAT_TIPONAYNO", "(K) Tip onay no", 50),
        ("RUHSAT_TCKIMLIK", "(Y.4) T.C. kimlik no / vergi no", 20),
        ("RUHSAT_SOYADIUNVAN", "(C.1.1) Soyadı / ticari ünvanı", 100),
        ("RUHSAT_ADI", "(C.1.2) Adı", 50),
        ("RUHSAT_ADRESI", "(C.1.3) Adresi", 250),
        ("RUHSAT_HACIZ", "(Z.1) Araç üzerinde hak ve menfaati bulunanlar", 250),
        ("RUHSAT_NOTERTARIH", "(Z.3.1) Noter satış tarihi", 20),
        ("RUHSAT_NOTERNO", "(Z.3.2) Noter satış no", 20),
        ("RUHSAT_NOTERINADI", "(Z.3.3) Noterin adı", 50),
        ("RUHSAT_DIGER", "(Z.2) Diğer bilgiler", 50),
        ("RUHSAT_ONAYLAYANSICIL", "(Y.5) Onaylayan sicil", 50),
        ("RUHSAT_BELGESERI", "Belge seri", 50),
    ];

    private const string SahipKosulu = "c.STATUS = 1 AND c.ID = @id AND c.ID IN (SELECT CLIENTID FROM userclients WHERE USERID = @kullaniciId)";
    private static readonly string RuhsatOku =
        $"SELECT c.PLAKA, {string.Join(", ", RuhsatAlanlari.Select(a => "c." + a.Kolon))} FROM clients c WITH (NOLOCK) WHERE {SahipKosulu}";
    private static readonly string RuhsatYaz =
        $"UPDATE c SET {string.Join(", ", RuhsatAlanlari.Select(a => $"{a.Kolon} = @{a.Kolon}"))} FROM clients c WHERE {SahipKosulu}";

    // Mesai: gün 1-7 (Pazartesi-Pazar), başlangıç/bitiş "SS:dd" ya da "-" (mesai yok).
    private static readonly string[] MesaiKolonlari = Enumerable.Range(1, 7).SelectMany(g => new[] { $"M{g}_1", $"M{g}_2" }).ToArray();
    private static readonly string MesaiOku =
        $"SELECT {string.Join(", ", MesaiKolonlari.Select(m => "c." + m))} FROM clients c WITH (NOLOCK) WHERE {SahipKosulu}";
    // Seçilen araçlara toplu uygulanır (eski formda olduğu gibi); @araclar virgüllü kimlik listesi (yalnız rakam).
    private static readonly string MesaiYaz = $"""
        UPDATE clients SET {string.Join(", ", MesaiKolonlari.Select(m => $"{m} = @{m}"))}
        WHERE STATUS = 1 AND ID IN (SELECT CLIENTID FROM userclients WHERE USERID = @kullaniciId)
          AND ID IN (SELECT n.x.value('.', 'int') FROM (SELECT CAST('<a>' + REPLACE(@araclar, ',', '</a><a>') + '</a>' AS xml) AS d) t CROSS APPLY t.d.nodes('/a') n(x))
        """;

    private const string KartListesi = """
        SELECT ID, KIMLIKID, ADSOYAD, EHLIYETNO, GSMNO, CREATEDATE FROM surucukimlik WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY KIMLIKID
        """;
    private const string KartGuncelle = """
        UPDATE surucukimlik SET ADSOYAD = @adSoyad, EHLIYETNO = @ehliyetNo, GSMNO = @gsmNo WHERE ID = @id AND USERID = @kullaniciId
        """;

    public sealed record MesaiIstegi(int[]? Araclar, string?[]? Saatler);
    public sealed record KartIstegi(string? AdSoyad, string? EhliyetNo, string? GsmNo);

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex SaatKalibi();

    public static void MapAyar(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/ayarlar").WithTags("Ayarlar").RequireAuthorization();

        // ---- Ruhsat ----
        g.MapGet("/ruhsat-alanlari", () => Results.Ok(RuhsatAlanlari.Select(a => new { ad = a.Kolon, baslik = a.Baslik, uz = a.Uz })));

        g.MapGet("/ruhsat/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AracYonetimi) return Yasak();
            var s = await db.TekSatirAsync(RuhsatOku, Arac(k, id), ct);
            if (s is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            return Results.Ok(new
            {
                plaka = RaporYardimci.Metin(s["PLAKA"]),
                alanlar = RuhsatAlanlari.ToDictionary(a => a.Kolon, a => (s[a.Kolon] as string)?.Trim() ?? ""),
            });
        }));

        g.MapPut("/ruhsat/{id:int}", (int id, Dictionary<string, string?> alanlar, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AracYonetimi) return Yasak();
            var p = Arac(k, id);
            foreach (var a in RuhsatAlanlari)
            {
                var v = (alanlar.GetValueOrDefault(a.Kolon) ?? "").Trim();
                if (v.Length > a.Uz) return Hata($"{a.Baslik}: en fazla {a.Uz} karakter.");
                p[a.Kolon] = v;
            }
            return await db.CalistirAsync(RuhsatYaz, p, ct) > 0
                ? Results.Ok(new { mesaj = "Ruhsat bilgileri kaydedildi." })
                : Results.NotFound(new { error = "Araç bulunamadı ya da size ait değil." });
        }));

        // ---- Mesai saatleri ----
        g.MapGet("/mesai/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var s = await db.TekSatirAsync(MesaiOku, Arac(k, id), ct);
            if (s is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            return Results.Ok(MesaiKolonlari.Select(m => Saat(s[m] as string)).ToArray());
        }));

        g.MapPut("/mesai", (MesaiIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var araclar = (i.Araclar ?? []).Where(x => x > 0).Distinct().ToArray();
            if (araclar.Length == 0) return Hata("En az bir araç seçiniz.");
            if (i.Saatler is not { Length: 14 }) return Hata("Mesai saatleri eksik.");
            var p = new Dictionary<string, object?> { ["kullaniciId"] = k.Id, ["araclar"] = string.Join(',', araclar) };
            for (var n = 0; n < 14; n++)
            {
                var v = Saat(i.Saatler[n]);
                if (v != "-" && !SaatKalibi().IsMatch(v)) return Hata("Saatler SS:dd biçiminde olmalı.");
                p[MesaiKolonlari[n]] = v;
            }
            // Bir günün yalnız bir ucu girilmişse ikisi de "-" sayılır.
            for (var gun = 0; gun < 7; gun++)
                if ((string)p[MesaiKolonlari[gun * 2]]! == "-" || (string)p[MesaiKolonlari[gun * 2 + 1]]! == "-")
                    p[MesaiKolonlari[gun * 2]] = p[MesaiKolonlari[gun * 2 + 1]] = "-";
            var n2 = await db.CalistirAsync(MesaiYaz, p, ct);
            return n2 > 0
                ? Results.Ok(new { mesaj = $"Mesai saatleri kaydedildi ({n2} araç)." })
                : Results.NotFound(new { error = "Seçilen araçlar bulunamadı ya da size ait değil." });
        }));

        // ---- RF-ID kartları ----
        g.MapGet("/rfid", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(KartListesi, new Dictionary<string, object?> { ["kullaniciId"] = k.Id }, ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                kartNo = (s["KIMLIKID"] as string ?? "").Trim(),
                adSoyad = RaporYardimci.Metin(s["ADSOYAD"]),
                ehliyetNo = RaporYardimci.Metin(s["EHLIYETNO"]),
                gsmNo = RaporYardimci.Metin(s["GSMNO"]),
                kayitTarihi = s["CREATEDATE"] as DateTime?,
            }))));

        g.MapPut("/rfid/{id:int}", (int id, KartIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            string Kes(string? s) { var t = (s ?? "").Trim(); return t.Length > 128 ? t[..128] : t; }
            return await db.CalistirAsync(KartGuncelle, new Dictionary<string, object?>
            {
                ["id"] = id, ["kullaniciId"] = k.Id, ["adSoyad"] = Kes(i.AdSoyad), ["ehliyetNo"] = Kes(i.EhliyetNo), ["gsmNo"] = Kes(i.GsmNo),
            }, ct) > 0
                ? Results.Ok(new { mesaj = "RFID kart bilgileri güncellendi." })
                : Results.NotFound(new { error = "Kart bulunamadı." });
        }));
    }

    private static async Task<IResult> Kullanici(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }

    private static Dictionary<string, object?> Arac(OturumKullanici k, int id) => new() { ["kullaniciId"] = k.Id, ["id"] = id };
    private static IResult Hata(string m) => Results.BadRequest(new { error = m });
    private static IResult Yasak() => Results.Json(new { error = "Bu işlem için yetkiniz yok." }, statusCode: StatusCodes.Status403Forbidden);
    private static string Saat(string? v) { var t = (v ?? "").Trim(); return t.Length == 0 ? "-" : t; }
}
