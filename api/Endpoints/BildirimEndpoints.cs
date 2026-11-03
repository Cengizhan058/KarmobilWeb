using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Bildirim (alarm) ayarları. Eski: js alarm_settings(), data/_getClientsAlarm.asp, _prcAlarm.asp, _alarmPolygons.asp,
/// _prcUserSettings.asp. Tanımlar eski tablolara aynı biçimde yazılır (gateway bunları okur):
///   cihazalarm: XE, DK, AA, BA (bölge, kullanıcı başına bölge satırları; NUM3 = kullanıcı), XZ, XD, XH
///   cihazsensor: XR, XI · cihazkomut: PH (hız alarmı - STATUS 0 ile cihaza gönderilir)
/// Alıcılar: users.EMAIL1-3 / GSM1-3; alarm başına mailalarm / smsalarm satırları.
/// Eski uçlarda sahiplik denetimi yoktu ve değerler SQL'e metin olarak ekleniyordu; burada araçlar userclients ile
/// sınırlı, değerler parametre ve her kayıt tek işlemde.
/// Fark: e-posta adresi değişince eski kod mailalarm satırlarını güncellemiyordu (kod yorum satırındaydı), alarmlar eski
/// adrese gitmeye devam ediyordu; burada GSM ile aynı biçimde güncellenir.
/// </summary>
public static class BildirimEndpoints
{
    public sealed record Secenek(string Deger, string Ad);

    /// <summary>Formdaki bir alarm. Sayi1/Sayi2: NUM2/NUM3 alanlarının başlığı ve alt sınırı; Saat: STR1-STR2 saat aralığı.</summary>
    public sealed record AlarmTanimi(string Kod, string Baslik, string Tablo, Secenek[] Durumlar,
        string? Sayi1 = null, int Sayi1Min = 0, string? Sayi2 = null, bool Saat = false, bool Bolge = false, bool Rota = false);

    private static readonly Secenek[] AcikKapali = [new("0", "Kapalı"), new("1", "Açık")];

    /// <summary>Eski form sırası. Kod: tablodaki KOMUT (bölge ihlali formda XB, tabloda BA).</summary>
    public static readonly AlarmTanimi[] Tanimlar =
    [
        new("PH", "Hız alarmı", "cihazkomut", AcikKapali, Sayi1: "Hız sınırı (km/s)", Sayi1Min: 1),
        new("XH", "Hareket alarmı", "cihazalarm", AcikKapali, Saat: true),
        new("XD", "Duraklama alarmı", "cihazalarm", AcikKapali, Sayi1: "Süre (sn)", Sayi1Min: 30, Saat: true),
        new("XR", "Rölanti alarmı", "cihazsensor", AcikKapali, Sayi1: "Süre (sn)", Sayi1Min: 120),
        new("BA", "Bölge ihlali alarmı", "cihazalarm",
            [new("0", "Kapalı"), new("1", "Sadece giriş"), new("2", "Sadece çıkış"), new("3", "Giriş ve çıkış")], Saat: true, Bolge: true),
        new("XZ", "Rota ihlali alarmı", "cihazalarm", AcikKapali, Saat: true, Rota: true),
        new("XE", "Enerji bağlantı durumu", "cihazalarm", AcikKapali),
        new("DK", "Kapı / kaput / depo kapağı alarmı", "cihazalarm", AcikKapali),
        new("AA", "Panik alarm", "cihazalarm", AcikKapali),
        new("XI", "Isı alarmı", "cihazsensor", AcikKapali, Sayi1: "En düşük (°C)", Sayi1Min: int.MinValue, Sayi2: "En yüksek (°C)", Saat: true),
    ];

    public sealed record AlarmAyari(string Kod, int Durum, double? Sayi1, double? Sayi2, string? Bas, string? Bit,
        int? Rota, int[]? Bolgeler, bool Eposta, bool Sms);

    public sealed record KayitIstegi(int[] Araclar, AlarmAyari[] Alarmlar);
    public sealed record AliciIstegi(bool Acik, string?[] Adresler);

    private const string AracSahip = """
        SELECT c.CIHAZID FROM clients c WITH (NOLOCK)
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND c.ID = @aracId
        """;

    private const string Alicilar = "SELECT EMAIL, EMAIL1, EMAIL2, EMAIL3, GSM, GSM1, GSM2, GSM3 FROM users WITH (NOLOCK) WHERE ID = @kullaniciId";

    // Aracın mevcut tanımları + bu kullanıcının adreslerine kayıtlı e-posta/SMS sayısı (eski _getClientsAlarm.asp).
    private const string AracAyarlari = """
        SELECT x.KOMUT, x.NUM1, x.NUM2, x.NUM3, x.STR1, x.STR2,
          (SELECT COUNT(*) FROM mailalarm m WITH (NOLOCK) WHERE m.CIHAZID = @cihazId AND m.ALARM = x.KOMUT
             AND m.EMAIL IN (u.EMAIL1, u.EMAIL2, u.EMAIL3)) AS MAIL,
          (SELECT COUNT(*) FROM smsalarm s WITH (NOLOCK) WHERE s.CIHAZID = @cihazId AND s.ALARM = x.KOMUT
             AND s.GSM IN (u.GSM1, u.GSM2, u.GSM3)) AS SMS
        FROM (
          SELECT KOMUT, NUM1, NUM2, NUM3, STR1, STR2 FROM cihazkomut WITH (NOLOCK) WHERE CIHAZID = @cihazId AND KOMUT = 'PH'
          UNION ALL SELECT KOMUT, NUM1, NUM2, NUM3, STR1, STR2 FROM cihazalarm WITH (NOLOCK)
            WHERE CIHAZID = @cihazId AND KOMUT IN ('XD', 'XH', 'XE', 'AA', 'XZ', 'DK')
          UNION ALL SELECT TOP 1 KOMUT, NUM1, NUM2, NUM3, STR1, STR2 FROM cihazalarm WITH (NOLOCK)
            WHERE CIHAZID = @cihazId AND KOMUT = 'BA' AND NUM3 = @kullaniciId
          UNION ALL SELECT KOMUT, NUM1, NUM2, NUM3, STR1, STR2 FROM cihazsensor WITH (NOLOCK) WHERE CIHAZID = @cihazId AND KOMUT IN ('XR', 'XI')
        ) x CROSS JOIN users u WITH (NOLOCK)
        WHERE u.ID = @kullaniciId
        """;

    private const string SeciliBolgeler = "SELECT NUM2 FROM cihazalarm WITH (NOLOCK) WHERE CIHAZID = @cihazId AND KOMUT = 'BA' AND NUM3 = @kullaniciId";

    private const string Bolgeler = "SELECT ID, REGION_NAME FROM regions WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY REGION_NAME";
    private const string Rotalar = "SELECT ID, TITLE FROM routes WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY TITLE";

    // Kayıt işleminin başı: seçilen araçlardan kullanıcınınkiler @a tablosuna alınır, sonraki her şey yalnız onlara yazar.
    private const string KayitBasi = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @a TABLE (CIHAZID nvarchar(64) PRIMARY KEY);
        INSERT @a SELECT DISTINCT c.CIHAZID FROM clients c WITH (NOLOCK)
          JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
          WHERE c.STATUS = 1 AND c.ID IN ({0});
        DECLARE @e TABLE (E nvarchar(250)); DECLARE @g TABLE (G nvarchar(20));
        INSERT @e SELECT v.E FROM users u CROSS APPLY (VALUES (u.EMAIL1), (u.EMAIL2), (u.EMAIL3)) v(E)
          WHERE u.ID = @kullaniciId AND v.E IS NOT NULL AND LTRIM(v.E) <> '' AND LEFT(v.E, 1) <> '-';
        INSERT @g SELECT v.G FROM users u CROSS APPLY (VALUES (u.GSM1), (u.GSM2), (u.GSM3)) v(G)
          WHERE u.ID = @kullaniciId AND v.G IS NOT NULL AND LTRIM(v.G) <> '' AND v.G <> '-';

        """;

    // Bir alarmın tanımı (BA dışı): araçta varsa güncellenir, yoksa eklenir. {0} tablo (yalnız Tanimlar'dan), {1} parametre öneki.
    private const string Tanim = """
        UPDATE x SET TARIH = CURRENT_TIMESTAMP, STR1 = @{1}s1, STR2 = @{1}s2, NUM1 = @{1}n1, NUM2 = @{1}n2, NUM3 = @{1}n3, NUM4 = @{1}n4, STATUS = 0
          FROM {0} x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.KOMUT = @{1}k;
        INSERT {0} (CIHAZID, KOMUT, STR1, STR2, NUM1, NUM2, NUM3, NUM4, STATUS)
          SELECT a.CIHAZID, @{1}k, @{1}s1, @{1}s2, @{1}n1, @{1}n2, @{1}n3, @{1}n4, 0 FROM @a a
          WHERE NOT EXISTS (SELECT 1 FROM {0} x WHERE x.CIHAZID = a.CIHAZID AND x.KOMUT = @{1}k);

        """;

    // Bölge ihlali: kullanıcının bu araçtaki bölge satırları silinip seçilen bölgeler (boşsa tüm bölgeleri) yazılır.
    private const string BolgeTanimi = """
        DELETE x FROM cihazalarm x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.KOMUT = 'BA' AND x.NUM3 = @kullaniciId;
        INSERT cihazalarm (CIHAZID, KOMUT, STR1, STR2, NUM1, NUM2, NUM3, NUM4, STATUS)
          SELECT a.CIHAZID, 'BA', @{0}s1, @{0}s2, @{0}n1, r.ID, @kullaniciId, 0, 0
          FROM @a a CROSS JOIN regions r WITH (NOLOCK)
          WHERE r.USERID = @kullaniciId {1};
        UPDATE c SET ROTABOLGESTATUS = 0, ROTABOLGETYPE = CASE WHEN @{0}n1 > 0 THEN 'B' + CAST(CAST(@{0}n1 AS int) AS nvarchar(2)) ELSE '-' END
          FROM clients c JOIN @a a ON a.CIHAZID = c.CIHAZID;

        """;

    // Alıcılar: bu kullanıcının adreslerine ait satırlar silinir, istenirse yeniden eklenir.
    private const string AlarmAlicilari = """
        DELETE m FROM mailalarm m JOIN @a a ON a.CIHAZID = m.CIHAZID WHERE m.ALARM = @{0}k AND m.EMAIL IN (SELECT E FROM @e);
        DELETE s FROM smsalarm s JOIN @a a ON a.CIHAZID = s.CIHAZID WHERE s.ALARM = @{0}k AND s.GSM IN (SELECT G FROM @g);
        IF @{0}mail = 1 INSERT mailalarm (CIHAZID, ALARM, EMAIL) SELECT a.CIHAZID, @{0}k, e.E FROM @a a CROSS JOIN @e e;
        IF @{0}sms = 1 INSERT smsalarm (CIHAZID, ALARM, GSM) SELECT a.CIHAZID, @{0}k, g.G FROM @a a CROSS JOIN @g g;

        """;

    private const string KayitSonu = "COMMIT; SELECT COUNT(*) FROM @a;";

    // Alıcı adresleri: kullanıcının araçlarındaki eski adresli alarm satırları yeni adrese taşınır, kapatılırsa silinir
    // (eski _prcUserSettings.asp GSM bölümü). {0}: mail/sms tablosu, {1}: kolon, {2}: users kolon öneki.
    private const string AliciGuncelle = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @a TABLE (CIHAZID nvarchar(64));
        INSERT @a SELECT c.CIHAZID FROM clients c WITH (NOLOCK) JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID WHERE u.USERID = @kullaniciId;
        DECLARE @o1 nvarchar(250), @o2 nvarchar(250), @o3 nvarchar(250);
        SELECT @o1 = {2}1, @o2 = {2}2, @o3 = {2}3 FROM users WHERE ID = @kullaniciId;
        IF @acik = 1
        BEGIN
          UPDATE x SET {1} = @y1 FROM {0} x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.{1} = @o1 AND @o1 <> '-' AND @y1 <> '-';
          UPDATE x SET {1} = @y2 FROM {0} x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.{1} = @o2 AND @o2 <> '-' AND @y2 <> '-';
          UPDATE x SET {1} = @y3 FROM {0} x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.{1} = @o3 AND @o3 <> '-' AND @y3 <> '-';
          DELETE x FROM {0} x JOIN @a a ON a.CIHAZID = x.CIHAZID
            WHERE (x.{1} = @o1 AND @y1 = '-') OR (x.{1} = @o2 AND @y2 = '-') OR (x.{1} = @o3 AND @y3 = '-');
        END
        ELSE
          DELETE x FROM {0} x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.{1} IN (@o1, @o2, @o3) AND x.{1} <> '-';
        UPDATE users SET {2} = @acikMetin, {2}1 = @y1, {2}2 = @y2, {2}3 = @y3 WHERE ID = @kullaniciId;
        COMMIT;
        """;

    private static readonly Regex EpostaBicimi = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static void MapBildirim(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/bildirim").WithTags("Bildirim ayarları").RequireAuthorization();

        g.MapGet("/tanimlar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var u = await db.TekSatirAsync(Alicilar, P(k), ct);
            string Adres(string ad) => RaporYardimci.Metin(u?[ad]) ?? "";
            return Results.Ok(new
            {
                alarmlar = Tanimlar.Select(t => new { t.Kod, t.Baslik, t.Durumlar, t.Sayi1, t.Sayi1Min, t.Sayi2, t.Saat, t.Bolge, t.Rota }),
                eposta = new { acik = Metin(u?["EMAIL"]) == "1", adresler = new[] { Adres("EMAIL1"), Adres("EMAIL2"), Adres("EMAIL3") } },
                sms = new { acik = Metin(u?["GSM"]) == "1", adresler = new[] { Adres("GSM1"), Adres("GSM2"), Adres("GSM3") } },
                bolgeler = (await db.SorguAsync(Bolgeler, P(k), ct)).Select(s => new { id = Convert.ToInt32(s["ID"]), ad = RaporYardimci.Metin(s["REGION_NAME"]) }),
                rotalar = (await db.SorguAsync(Rotalar, P(k), ct)).Select(s => new { id = Convert.ToInt32(s["ID"]), ad = RaporYardimci.Metin(s["TITLE"]) }),
            });
        }));

        g.MapGet("/araclar/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var cihaz = await db.TekDegerAsync<string>(AracSahip, new Dictionary<string, object?>(P(k)) { ["aracId"] = id }, ct);
            if (cihaz is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            var p = new Dictionary<string, object?>(P(k)) { ["cihazId"] = cihaz };
            var satirlar = await db.SorguAsync(AracAyarlari, p, ct);
            var bolgeler = (await db.SorguAsync(SeciliBolgeler, p, ct)).Select(s => Convert.ToInt32(s["NUM2"])).ToArray();
            return Results.Ok(satirlar.GroupBy(s => (s["KOMUT"] as string ?? "").Trim()).Select(gr =>
            {
                var s = gr.First();
                var t = Tanimlar.FirstOrDefault(x => x.Kod == gr.Key);
                var n2 = RaporYardimci.Ondalik(s["NUM2"]);
                return new AlarmAyari(gr.Key, (int)(RaporYardimci.Ondalik(s["NUM1"]) ?? 0),
                    gr.Key is "BA" or "XZ" ? null : n2,
                    gr.Key == "XI" ? RaporYardimci.Ondalik(s["NUM3"]) : null,
                    Saat(s["STR1"]), Saat(s["STR2"]),
                    gr.Key == "XZ" && n2 is { } r ? (int)r : null,
                    gr.Key == "BA" ? bolgeler : null,
                    Convert.ToInt32(s["MAIL"]) > 0, Convert.ToInt32(s["SMS"]) > 0);
            }));
        }));

        g.MapPut("", (KayitIstegi istek, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var araclar = (istek.Araclar ?? []).Where(x => x > 0).Distinct().ToArray();
            if (araclar.Length == 0) return Hata("En az bir araç seçiniz.");
            if (araclar.Length > 1000) return Hata("En fazla 1000 araç seçilebilir.");
            if (istek.Alarmlar is not { Length: > 0 }) return Hata("Kaydedilecek alarm seçiniz.");

            var p = P(k);
            var sql = new StringBuilder();
            var adlar = new string[araclar.Length];
            for (var i = 0; i < araclar.Length; i++) { p["i" + i] = araclar[i]; adlar[i] = "@i" + i; }
            sql.AppendFormat(CultureInfo.InvariantCulture, KayitBasi, string.Join(",", adlar));

            var j = 0;
            foreach (var a in istek.Alarmlar.DistinctBy(x => x.Kod))
            {
                var t = Tanimlar.FirstOrDefault(x => x.Kod == a.Kod);
                if (t is null) return Hata($"Bilinmeyen alarm: {a.Kod}");
                if (!t.Durumlar.Any(d => d.Deger == a.Durum.ToString(CultureInfo.InvariantCulture))) return Hata($"{t.Baslik}: geçersiz durum.");
                var acik = a.Durum > 0;
                if (acik && t.Sayi1 is not null && (a.Sayi1 is null || a.Sayi1 < t.Sayi1Min))
                    return Hata(a.Kod switch
                    {
                        "XR" => "Rölantide bekleme süresi 120 sn'den küçük olamaz.",
                        "XD" => "Duraklama alarmı için bekleme süresi 30 sn'den küçük olamaz.",
                        _ => $"{t.Baslik}: {t.Sayi1} giriniz.",
                    });
                if (acik && t.Sayi2 is not null && a.Sayi2 is null) return Hata($"{t.Baslik}: {t.Sayi2} giriniz.");
                if (acik && a.Kod == "XI" && a.Sayi2 < a.Sayi1) return Hata("Isı alarmı: en yüksek değer en düşükten küçük olamaz.");
                if (acik && t.Rota && a.Rota is not > 0) return Hata("Rota ihlali alarmı için bir rota seçiniz.");
                if (t.Saat && (!SaatGecerli(a.Bas) || !SaatGecerli(a.Bit))) return Hata($"{t.Baslik}: geçersiz saat.");

                var on = "p" + j++ + "_";
                p[on + "k"] = a.Kod;
                p[on + "s1"] = t.Saat ? a.Bas ?? "-" : "-";
                p[on + "s2"] = t.Saat ? a.Bit ?? "-" : "-";
                p[on + "n1"] = (double)a.Durum;
                // NUM2-4 eski _prcAlarm.asp'deki gibi: PH hız + 30 + 5, XI min/maks, XZ rota, XD/XR süre.
                p[on + "n2"] = a.Kod switch { "XZ" => (double)(a.Rota ?? 0), "PH" or "XD" or "XR" or "XI" => a.Sayi1 ?? 0, _ => 0d };
                p[on + "n3"] = a.Kod switch { "PH" => 30d, "XI" => a.Sayi2 ?? 0, _ => 0d };
                p[on + "n4"] = a.Kod == "PH" ? 5d : 0d;
                p[on + "mail"] = a.Eposta ? 1 : 0;
                p[on + "sms"] = a.Sms ? 1 : 0;

                if (a.Kod == "BA")
                {
                    var secili = (a.Bolgeler ?? []).Where(x => x > 0).Distinct().Take(1000).ToArray();
                    var kosul = "";
                    if (secili.Length > 0)
                    {
                        for (var b = 0; b < secili.Length; b++) p[on + "b" + b] = secili[b];
                        kosul = "AND r.ID IN (" + string.Join(",", secili.Select((_, b) => "@" + on + "b" + b)) + ")";
                    }
                    sql.AppendFormat(CultureInfo.InvariantCulture, BolgeTanimi, on, kosul);
                }
                else sql.AppendFormat(CultureInfo.InvariantCulture, Tanim, t.Tablo, on);
                sql.AppendFormat(CultureInfo.InvariantCulture, AlarmAlicilari, on);
            }
            sql.Append(KayitSonu);

            var adet = await db.TekDegerAsync<int>(sql.ToString(), p, ct);
            return adet == 0 ? Results.NotFound(new { error = "Araç bulunamadı." })
                : Results.Ok(new { mesaj = $"Alarm tanımlamaları kaydedildi ({adet} araç)." });
        }));

        g.MapPut("/alicilar/{tur}", (string tur, AliciIstegi istek, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var eposta = tur == "eposta";
            if (!eposta && tur != "sms") return Results.NotFound();
            var adresler = (istek.Adresler ?? []).Concat(Enumerable.Repeat<string?>(null, 3)).Take(3)
                .Select(x => string.IsNullOrWhiteSpace(x) ? "-" : x.Trim()).ToArray();
            for (var i = 0; i < 3; i++)
            {
                if (adresler[i] == "-") continue;
                if (eposta && (adresler[i].Length > 250 || !EpostaBicimi.IsMatch(adresler[i]))) return Hata($"{i + 1}. e-posta adresi geçersiz.");
                if (!eposta)
                {
                    // Eski formda başında (0) yazılı, 10 hane bekleniyordu (5xx xxx xx xx).
                    var gsm = Regex.Replace(adresler[i], @"\D", "").TrimStart('0');
                    if (gsm.Length != 10) return Hata($"{i + 1}. GSM numarası 10 haneli olmalı (5xx xxx xx xx).");
                    adresler[i] = gsm;
                }
            }
            var p = new Dictionary<string, object?>(P(k))
            {
                ["acik"] = istek.Acik ? 1 : 0, ["acikMetin"] = istek.Acik ? "1" : "0",
                ["y1"] = adresler[0], ["y2"] = adresler[1], ["y3"] = adresler[2],
            };
            await db.CalistirAsync(string.Format(CultureInfo.InvariantCulture, AliciGuncelle,
                eposta ? "mailalarm" : "smsalarm", eposta ? "EMAIL" : "GSM", eposta ? "EMAIL" : "GSM"), p, ct);
            return Results.Ok(new { mesaj = eposta ? "E-posta bilgileri güncellendi." : "GSM bilgileri güncellendi." });
        }));
    }

    private static string? Metin(object? v) => (v as string ?? v?.ToString())?.Trim();

    /// <summary>STR1/STR2: "-" ya da 0-23 saat.</summary>
    private static string? Saat(object? v) => Metin(v) is { Length: > 0 } s && s != "-" ? s : null;
    private static bool SaatGecerli(string? s) => s is null or "-" || (int.TryParse(s, out var n) && n is >= 0 and <= 23);

    private static Dictionary<string, object?> P(OturumKullanici k) => new() { ["kullaniciId"] = k.Id };
    private static IResult Hata(string m) => Results.BadRequest(new { error = m });

    private static async Task<IResult> Kullanici(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }
}
