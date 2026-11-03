using System.Globalization;
using System.Text.RegularExpressions;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Cihaza giden ayarlar. Hepsi yalnız kullanıcının kendi (userclients) araçlarına yazar; eski uçlarda sahiplik denetimi yoktu.
///   Cihaz programlama (eski device_settings, _getClientsProgram.asp, _prcProgram.asp): cihazkomut PZ / PD / PM / PH,
///     STATUS 0 = gateway cihaza gönderir. Yetki: users.YP. Eski formdaki uyku modu (KU) gönderilmiyordu (kod hatası), alınmadı;
///     pil sıfırlama alınmadı (pil/şarj özelliği kullanılmıyor).
///   Akıllı motor blokaj (smart_block, _getSmartBlock.asp, _prcSmartBlock.asp): motorbloke tablosunda haftalık saat planı.
///     Yetki: users.AKILLIBLOKE.
///   Motor blokaj (bilgi panelindeki düğme, _prcBloke.asp): cihazkomut 'CIKIS4 AC' / 'CIKIS4 KAPAT' + dbo.trcRequest.
///     Yalnız blokaj donanımı olan araçta (clients.MOTOR 0 / 1).
/// </summary>
public static partial class CihazEndpoints
{
    public sealed record ProgramAyari(string Kod, int Durum, double? Deger);
    public sealed record ProgramIstegi(int[] Araclar, ProgramAyari[] Komutlar);
    public sealed record BlokajGunu(string? Bas, string? Bit);
    public sealed record BlokajIstegi(int[] Araclar, BlokajGunu[] Gunler);
    public sealed record MotorIstegi(bool Kilitle);

    /// <summary>Kod, başlık, birim, alt sınır (eski formdaki uyarılar).</summary>
    public static readonly (string Kod, string Baslik, string Birim, double Min)[] Programlar =
    [
        ("PZ", "Hareketliyken veri gönderimi", "sn", 10),
        ("PD", "Dururken veri gönderimi", "sn", 3600),
        ("PM", "Mesafeye göre veri gönderimi", "km", 1),
        ("PH", "Hız limiti", "km/s", 1),
    ];

    private const string Sahip = """
        SELECT c.CIHAZID, c.MOTOR FROM clients c WITH (NOLOCK)
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND c.ID = @aracId
        """;

    // PD'de süre NUM4'te (NUM2 = 5, NUM3 = 15 sabit), diğerlerinde NUM2.
    private const string ProgramOku = """
        SELECT KOMUT, NUM1, CASE WHEN KOMUT = 'PD' THEN NUM4 ELSE NUM2 END AS DEGER, STATUS, TARIH
        FROM cihazkomut WITH (NOLOCK) WHERE CIHAZID = @cihazId AND KOMUT IN ('PZ', 'PD', 'PM', 'PH')
        """;

    private const string AracTablosu = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @a TABLE (CIHAZID nvarchar(64) PRIMARY KEY, ID int);
        INSERT @a SELECT c.CIHAZID, MIN(c.ID) FROM clients c WITH (NOLOCK)
          JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
          WHERE c.STATUS = 1 AND c.ID IN ({0}) GROUP BY c.CIHAZID;

        """;

    // Eski setData: varsa güncellenir, yoksa eklenir; STATUS 0 = gönderilecek. {0}: parametre öneki.
    private const string ProgramYaz = """
        UPDATE x SET UID = @kullaniciId, TARIH = CURRENT_TIMESTAMP, STR1 = '-', STR2 = '-',
               NUM1 = @{0}n1, NUM2 = @{0}n2, NUM3 = @{0}n3, NUM4 = @{0}n4, STATUS = 0
          FROM cihazkomut x JOIN @a a ON a.CIHAZID = x.CIHAZID WHERE x.KOMUT = @{0}k;
        INSERT cihazkomut (CIHAZID, KOMUT, STR1, STR2, NUM1, NUM2, NUM3, NUM4, STATUS, UID)
          SELECT a.CIHAZID, @{0}k, '-', '-', @{0}n1, @{0}n2, @{0}n3, @{0}n4, 0, @kullaniciId FROM @a a
          WHERE NOT EXISTS (SELECT 1 FROM cihazkomut x WHERE x.CIHAZID = a.CIHAZID AND x.KOMUT = @{0}k);

        """;

    private const string BlokajOku = """
        SELECT TOP 1 m.M1_1, m.M1_2, m.M2_1, m.M2_2, m.M3_1, m.M3_2, m.M4_1, m.M4_2, m.M5_1, m.M5_2, m.M6_1, m.M6_2, m.M7_1, m.M7_2
        FROM motorbloke m WITH (NOLOCK)
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = m.CLIENTID AND u.USERID = @kullaniciId
        WHERE m.CLIENTID = @aracId
        """;

    private const string BlokajYaz = """
        DELETE m FROM motorbloke m WHERE m.CLIENTID IN (SELECT ID FROM @a);
        INSERT motorbloke (CLIENTID, M1_1, M1_2, M2_1, M2_2, M3_1, M3_2, M4_1, M4_2, M5_1, M5_2, M6_1, M6_2, M7_1, M7_2)
          SELECT a.ID, @m1_1, @m1_2, @m2_1, @m2_2, @m3_1, @m3_2, @m4_1, @m4_2, @m5_1, @m5_2, @m6_1, @m6_2, @m7_1, @m7_2 FROM @a a;

        """;

    private const string Son = "COMMIT; SELECT COUNT(*) FROM @a;";

    // Eski _prcBloke.asp ile aynı: tek CIKIS4 satırı güncellenir ya da eklenir, ardından trcRequest komutu gönderir ve clients.MOTOR'u günceller.
    private const string MotorKomutu = """
        IF EXISTS (SELECT 1 FROM cihazkomut WHERE CIHAZID = @cihazId AND KOMUT IN ('CIKIS4 AC', 'CIKIS4 KAPAT'))
            UPDATE cihazkomut SET UID = @kullaniciId, TARIH = GETDATE(), KOMUT = @komut, STATUS = 0
            WHERE CIHAZID = @cihazId AND KOMUT IN ('CIKIS4 AC', 'CIKIS4 KAPAT');
        ELSE
            INSERT INTO cihazkomut (CIHAZID, KOMUT, STR1, STR2, NUM1, NUM2, NUM3, NUM4, STATUS, TARIH, UID)
            VALUES (@cihazId, @komut, '-', '-', 0, 0, 0, 0, 0, GETDATE(), @kullaniciId);
        EXEC dbo.trcRequest @cihazId, @komut;
        """;

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex SaatBicimi();

    public static void MapCihaz(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/cihaz").WithTags("Cihaz").RequireAuthorization();

        g.MapGet("/program/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.CihazProgramlama) return Yasak();
            var arac = await db.TekSatirAsync(Sahip, Arac(k, id), ct);
            if (arac is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            var satirlar = await db.SorguAsync(ProgramOku, new Dictionary<string, object?> { ["cihazId"] = arac["CIHAZID"] }, ct);
            return Results.Ok(Programlar.Select(p =>
            {
                var s = satirlar.FirstOrDefault(x => (x["KOMUT"] as string ?? "").Trim() == p.Kod);
                return new
                {
                    kod = p.Kod, baslik = p.Baslik, birim = p.Birim, min = p.Min,
                    durum = s is null ? 0 : (int)(RaporYardimci.Ondalik(s["NUM1"]) ?? 0),
                    deger = s is null ? (double?)null : RaporYardimci.Ondalik(s["DEGER"]),
                    // STATUS 0: henüz cihaza gitmedi.
                    bekliyor = s is not null && Convert.ToInt64(s["STATUS"] ?? 0L) == 0,
                    tarih = s?["TARIH"],
                };
            }));
        }));

        g.MapPut("/program", (ProgramIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.CihazProgramlama) return Yasak();
            if (i.Komutlar is not { Length: > 0 }) return Hata("Gönderilecek ayar seçiniz.");
            var (p, sql, hata) = AracBasi(k, i.Araclar);
            if (hata is not null) return hata;
            var j = 0;
            foreach (var c in i.Komutlar.DistinctBy(x => x.Kod))
            {
                var t = Programlar.FirstOrDefault(x => x.Kod == c.Kod);
                if (t.Kod is null) return Hata($"Bilinmeyen ayar: {c.Kod}");
                if (c.Durum is not (0 or 1)) return Hata($"{t.Baslik}: geçersiz durum.");
                if (c.Deger is null || c.Deger < t.Min || c.Deger > 1_000_000)
                    return Hata($"{t.Baslik}: değer {t.Min.ToString(CultureInfo.InvariantCulture)} {t.Birim}'den küçük olamaz.");
                var on = "p" + j++ + "_";
                p[on + "k"] = c.Kod;
                p[on + "n1"] = (double)c.Durum;
                (p[on + "n2"], p[on + "n3"], p[on + "n4"]) = c.Kod switch
                {
                    "PD" => (5d, 15d, c.Deger.Value),
                    "PH" => (c.Deger.Value, 30d, 5d),
                    _ => (c.Deger.Value, 0d, 0d),
                };
                sql += string.Format(CultureInfo.InvariantCulture, ProgramYaz, on);
            }
            var adet = await db.TekDegerAsync<int>(sql + Son, p, ct);
            return adet == 0 ? Results.NotFound(new { error = "Araç bulunamadı." }) : Results.Ok(new { mesaj = $"Ayarlar cihazlara gönderildi ({adet} araç)." });
        }));

        g.MapGet("/akilli-blokaj/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AkilliBlokaj) return Yasak();
            if (await db.TekSatirAsync(Sahip, Arac(k, id), ct) is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            var s = await db.TekSatirAsync(BlokajOku, Arac(k, id), ct);
            return Results.Ok(Enumerable.Range(1, 7).Select(gun => new BlokajGunu(Saat(s?[$"M{gun}_1"]), Saat(s?[$"M{gun}_2"]))));
        }));

        g.MapPut("/akilli-blokaj", (BlokajIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AkilliBlokaj) return Yasak();
            if (i.Gunler is not { Length: 7 }) return Hata("Yedi günün saatleri gönderilmeli.");
            var (p, sql, hata) = AracBasi(k, i.Araclar);
            if (hata is not null) return hata;
            for (var gun = 1; gun <= 7; gun++)
            {
                var gb = i.Gunler[gun - 1];
                var bas = string.IsNullOrEmpty(gb.Bas) ? "-" : gb.Bas;
                var bit = string.IsNullOrEmpty(gb.Bit) ? "-" : gb.Bit;
                if ((bas == "-") != (bit == "-")) return Hata("Blokaj için başlangıç ve bitiş saatini birlikte giriniz.");
                if (bas != "-" && (!SaatBicimi().IsMatch(bas) || !SaatBicimi().IsMatch(bit))) return Hata("Saat SS:DD biçiminde olmalı.");
                p[$"m{gun}_1"] = bas;
                p[$"m{gun}_2"] = bit;
            }
            var adet = await db.TekDegerAsync<int>(sql + BlokajYaz + Son, p, ct);
            return adet == 0 ? Results.NotFound(new { error = "Araç bulunamadı." }) : Results.Ok(new { mesaj = $"Motor blokaj ayarları kaydedildi ({adet} araç)." });
        }));

        app.MapPost("/api/araclar/{id:int}/motor-blokaj", (int id, MotorIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var arac = await db.TekSatirAsync(Sahip, Arac(k, id), ct);
            if (arac is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            if ((arac["MOTOR"]?.ToString() ?? "").Trim() is not ("0" or "1")) return Hata("Bu araçta motor blokajı yok.");
            await db.CalistirAsync(MotorKomutu, new Dictionary<string, object?>
            {
                ["kullaniciId"] = k.Id, ["cihazId"] = arac["CIHAZID"], ["komut"] = i.Kilitle ? "CIKIS4 AC" : "CIKIS4 KAPAT",
            }, ct);
            return Results.Ok(new { mesaj = i.Kilitle ? "Motor kilitleme komutu cihaza gönderildi." : "Motor açma komutu cihaza gönderildi." });
        })).RequireAuthorization();
    }

    /// <summary>Seçilen araçlardan kullanıcınınkileri @a tablosuna alan işlem başı.</summary>
    private static (Dictionary<string, object?> P, string Sql, IResult? Hata) AracBasi(OturumKullanici k, int[]? araclar)
    {
        var liste = (araclar ?? []).Where(x => x > 0).Distinct().ToArray();
        if (liste.Length == 0) return ([], "", Hata("En az bir araç seçiniz."));
        if (liste.Length > 1000) return ([], "", Hata("En fazla 1000 araç seçilebilir."));
        var p = new Dictionary<string, object?> { ["kullaniciId"] = k.Id };
        for (var i = 0; i < liste.Length; i++) p["i" + i] = liste[i];
        return (p, string.Format(CultureInfo.InvariantCulture, AracTablosu, string.Join(",", liste.Select((_, i) => "@i" + i))), null);
    }

    private static string? Saat(object? v) => (v as string)?.Trim() is { Length: > 0 } s && s != "-" ? s : null;
    private static Dictionary<string, object?> Arac(OturumKullanici k, int id) => new() { ["kullaniciId"] = k.Id, ["aracId"] = id };
    private static IResult Hata(string m) => Results.BadRequest(new { error = m });
    private static IResult Yasak() => Results.Json(new { error = "Bu işlem için yetkiniz yok." }, statusCode: StatusCodes.Status403Forbidden);

    private static async Task<IResult> Kullanici(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }
}
