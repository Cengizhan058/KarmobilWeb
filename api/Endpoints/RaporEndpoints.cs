using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Genel rapor uçları. Rapor tanımları Raporlar/ klasöründeki IRapor sınıflarından gelir.
///   GET /api/raporlar              → tanımlar (form oluşturmak için)
///   GET /api/raporlar/{kod}?araclar=1,2&amp;baslangic=..&amp;bitis=..&amp;ay=2026-11&amp;p_psure=30
/// </summary>
public static class RaporEndpoints
{
    // Kullanıcının erişebildiği araçlar (userclients + usermobiles), seçilen kimliklerle sınırlı.
    private const string ErisilenAraclar = """
        SELECT c.ID, c.CIHAZID, c.PLAKA FROM clients c WITH (NOLOCK)
        WHERE c.STATUS = 1 AND c.ID IN (
            SELECT CLIENTID FROM userclients WITH (NOLOCK) WHERE USERID = @kullaniciId
            UNION SELECT CLIENTID FROM usermobiles WITH (NOLOCK) WHERE USERID = @kullaniciId)
        ORDER BY c.PLAKA
        """;

    // Gider raporlarında: kullanıcının gider takibi açık araçları (eski fms formlarındaki liste).
    private const string GiderAraclari = """
        SELECT c.ID, c.CIHAZID, c.PLAKA FROM clients c WITH (NOLOCK)
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND c.FMS = 1
        ORDER BY c.PLAKA
        """;

    // Seçim listeleri (eski _forms_raporX.asp'lerdeki DB'den dolan select'ler). Hepsi kullanıcıya göre süzülür.
    private const string KatmanKosulu = "(l.USERID = @kullaniciId OR (l.SUBUSERS = 1 AND l.USERID = (SELECT GROUPID FROM users WITH (NOLOCK) WHERE ID = @kullaniciId)))";
    private static readonly Dictionary<string, string> SecenekSorgulari = new(StringComparer.OrdinalIgnoreCase)
    {
        ["alarmTurleri"] = "SELECT RAPORKODU AS DEGER, RAPORADI AS AD FROM raportanim WITH (NOLOCK) ORDER BY RAPORADI",
        ["suruculer"] = "SELECT KIMLIKID AS DEGER, ADSOYAD AS AD FROM surucukimlik WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY ADSOYAD",
        ["komutKodlari"] = "SELECT KOMUT AS DEGER, ACIKLAMA AS AD FROM cihazkomutkod WITH (NOLOCK) ORDER BY ACIKLAMA",
        ["canbusGirisleri"] = "SELECT KOMUT AS DEGER, TITLE AS AD FROM canbusinputs WITH (NOLOCK) ORDER BY TITLE",
        ["bolgeler"] = "SELECT ID AS DEGER, REGION_NAME AS AD FROM regions WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY REGION_NAME",
        ["gruplar"] = "SELECT ID AS DEGER, GROUPNAME AS AD FROM groups WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY GROUPNAME",
        ["katmanlar"] = $"SELECT l.ID AS DEGER, l.LAYERNAME AS AD FROM layers l WITH (NOLOCK) WHERE {KatmanKosulu} ORDER BY l.LAYERNAME",
        ["noktalar"] = $"SELECT u.ID AS DEGER, CONCAT(l.LAYERNAME, ' / ', u.TITLE) AS AD FROM userpoints u WITH (NOLOCK) JOIN layers l WITH (NOLOCK) ON l.ID = u.LAYERID WHERE {KatmanKosulu} ORDER BY l.LAYERNAME, u.TITLE",
    };

    public static void MapRapor(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/raporlar").WithTags("Raporlar").RequireAuthorization();

        grup.MapGet("/secenekler/{kaynak}", async (string kaynak, HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            if (!SecenekSorgulari.TryGetValue(kaynak, out var sql)) return Results.NotFound(new { error = "Bilinmeyen seçenek kaynağı." });
            var satirlar = await db.SorguAsync(sql, new Dictionary<string, object?> { ["kullaniciId"] = k.Id }, ct);
            return Results.Ok(satirlar
                .Select(s => new RaporSecenek((s["DEGER"]?.ToString() ?? "").Trim(), RaporYardimci.Metin(s["AD"]) ?? ""))
                .Where(s => s.Deger.Length > 0));
        });

        grup.MapGet("", async (HttpContext http, OturumServisi oturum, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            return Results.Ok(RaporKaydi.Tumu.Values.OrderBy(r => r.Baslik).Select(r => new
            {
                kod = r.Kod,
                baslik = r.Baslik,
                arac = r.Arac.ToString().ToLowerInvariant(),
                tarih = r.Tarih.ToString().ToLowerInvariant(),
                azamiGun = r.AzamiGun,
                gider = r.Gider,
                parametreler = r.Parametreler,
            }));
        });

        grup.MapGet("/{kod}", async (string kod, HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            if (!RaporKaydi.Tumu.TryGetValue(kod, out var rapor))
                return Results.NotFound(new { error = "Bu rapor yeni arayüze henüz taşınmadı." });
            if (rapor.Gider ? !k.GiderYonetimi : !k.Raporlar && !kod.Equals("report_last", StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "Rapor yetkiniz yok." }, statusCode: StatusCodes.Status403Forbidden);

            var q = http.Request.Query;

            // Araçlar: yalnızca kullanıcının erişebildikleri; "tumu" ya da boş → hepsi (çoklu raporlarda).
            var istenen = (q["araclar"].ToString() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0).ToHashSet();
            var erisilen = (await db.SorguAsync(rapor.Gider ? GiderAraclari : ErisilenAraclar, new Dictionary<string, object?> { ["kullaniciId"] = k.Id }, ct))
                .Select(s => new RaporAraci(Convert.ToInt32(s["ID"]), (s["CIHAZID"] as string ?? "").Trim(), (s["PLAKA"] as string ?? "").Trim()))
                .ToList();
            var araclar = rapor.Arac == AracSecimi.Yok ? []
                : istenen.Count == 0 && rapor.Arac == AracSecimi.Cok ? erisilen
                : erisilen.Where(a => istenen.Contains(a.Id)).ToList();

            if (rapor.Arac == AracSecimi.Tek && araclar.Count != 1) return Results.BadRequest(new { error = "Araç seçiniz." });
            if (rapor.Arac == AracSecimi.Cok && araclar.Count == 0) return Results.BadRequest(new { error = "En az bir araç seçiniz." });

            DateTime? bas = null, bit = null, ay = null;
            if (rapor.Tarih == TarihSecimi.Aralik)
            {
                if (!DateTime.TryParse(q["baslangic"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var b) ||
                    !DateTime.TryParse(q["bitis"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var s))
                    return Results.BadRequest(new { error = "Tarih aralığı belirtiniz." });
                if (s <= b) return Results.BadRequest(new { error = "Hatalı tarih aralığı." });
                if (rapor.AzamiGun > 0 && (s - b).TotalDays > rapor.AzamiGun)
                    return Results.BadRequest(new { error = $"Tarih aralığı en fazla {rapor.AzamiGun} gün olabilir." });
                bas = b; bit = s;
            }
            else if (rapor.Tarih == TarihSecimi.Ay)
            {
                if (!DateTime.TryParseExact(q["ay"], "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var a))
                    return Results.BadRequest(new { error = "Ay seçiniz." });
                ay = a;
            }

            var ekstra = rapor.Parametreler.ToDictionary(p => p.Ad, p => (string?)(q["p_" + p.Ad].ToString() is { Length: > 0 } v ? v : p.Varsayilan));

            RaporSonuc sonuc;
            try
            {
                sonuc = await rapor.CalistirAsync(new RaporIstegi
                {
                    Kullanici = k, Araclar = araclar, TumAraclar = istenen.Count == 0 && rapor.Arac == AracSecimi.Cok, Baslangic = bas, Bitis = bit, Ay = ay, Ekstra = ekstra,
                }, db, ct);
            }
            catch (RaporHatasi h) { return Results.BadRequest(new { error = h.Message }); }

            return Results.Ok(new { kod = rapor.Kod, baslik = rapor.Baslik, sonuc.Kolonlar, sonuc.Satirlar, sonuc.Ozet });
        });
    }
}
