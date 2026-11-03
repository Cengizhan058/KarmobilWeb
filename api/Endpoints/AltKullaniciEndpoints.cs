using System.Text.RegularExpressions;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Ayarlar → Kullanıcı Yetkilendirme: alt kullanıcı hesapları (eski: js sub_users(), data/_getUsers.asp,
/// _getUserClients.asp, _prcUsers.asp). Alt kullanıcı users.GROUPID = ana kullanıcı, USERLEVEL 'g'; araçları userclients.
/// Eski uçtaki açıklar kapatıldı: alt kullanıcıya ana kullanıcının olmayan araç da atanabiliyordu, kullanıcı listesi
/// şifreleri tarayıcıya gönderiyordu, değerler SQL'e metin olarak ekleniyordu.
/// Farklar: alt kullanıcıya yalnız ana kullanıcının kendi yetkileri verilebilir; yeni hesap sayısı USERLIMIT ile sınırlı
/// (eskisi yalnız USERLIMIT &gt; 0 bakıyordu). Şifreler eski site ve mobil uygulama giriş yaptığı için düz metin kalır.
/// </summary>
public static class AltKullaniciEndpoints
{
    public sealed record Yetkiler(bool Harita, bool AracYonetimi, bool CihazProgramlama, bool GrupYonetimi, bool Raporlar);
    public sealed record AltKullaniciIstegi(string? KullaniciAdi, string? Sifre, Yetkiler? Yetkiler, int[]? Araclar);

    private const string Liste = """
        SELECT u.ID, u.USERNAME, u.YW, u.YA, u.YP, u.YG, u.YR,
               (SELECT COUNT(*) FROM userclients uc WITH (NOLOCK) WHERE uc.USERID = u.ID) AS ARACSAYISI
        FROM users u WITH (NOLOCK) WHERE u.GROUPID = @kullaniciId ORDER BY u.USERNAME
        """;

    private const string Araclari = """
        SELECT uc.CLIENTID FROM userclients uc WITH (NOLOCK)
        JOIN users u WITH (NOLOCK) ON u.ID = uc.USERID AND u.GROUPID = @kullaniciId
        WHERE uc.USERID = @id
        """;

    private const string AdVarMi = "SELECT COUNT(*) FROM users WITH (NOLOCK) WHERE USERNAME = @ad AND ID <> @id";
    private const string AltSayisi = "SELECT COUNT(*) FROM users WITH (NOLOCK) WHERE GROUPID = @kullaniciId";

    // Araç atama: yalnız ana kullanıcının araçları (eskide denetim yoktu). @hedef alt kullanıcı, {0} araç parametreleri.
    private const string AraclariYaz = """
        DELETE FROM userclients WHERE USERID = @hedef;
        INSERT userclients (USERID, CLIENTID)
          SELECT DISTINCT @hedef, uc.CLIENTID FROM userclients uc WITH (NOLOCK)
          WHERE uc.USERID = @kullaniciId AND uc.CLIENTID IN ({0});
        """;

    private const string Ekle = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @hedef int;
        INSERT INTO users (USERNAME, PASSWORD, USERLEVEL, YC, YW, YG, YA, YP, YR, GROUPID, FIRMAKODU)
          SELECT @ad, @sifre, 'g', 0, @yw, @yg, @ya, @yp, @yr, @kullaniciId, FIRMAKODU FROM users WHERE ID = @kullaniciId;
        SET @hedef = SCOPE_IDENTITY();
        {0}
        COMMIT;
        SELECT @hedef;
        """;

    private const string Guncelle = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @hedef int = @id;
        UPDATE users SET USERNAME = @ad, PASSWORD = CASE WHEN @sifre IS NULL THEN PASSWORD ELSE @sifre END,
               YC = 0, YW = @yw, YG = @yg, YA = @ya, YP = @yp, YR = @yr
          WHERE ID = @id AND GROUPID = @kullaniciId;
        IF @@ROWCOUNT = 1
        BEGIN
          {0}
          COMMIT; SELECT 1;
        END
        ELSE BEGIN ROLLBACK; SELECT 0; END
        """;

    private const string Sil = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DELETE FROM userclients WHERE USERID = (SELECT ID FROM users WHERE ID = @id AND GROUPID = @kullaniciId);
        DELETE FROM users WHERE ID = @id AND GROUPID = @kullaniciId;
        SELECT @@ROWCOUNT;
        COMMIT;
        """;

    private static readonly Regex AdBicimi = new(@"^\S{3,150}$", RegexOptions.Compiled);

    public static void MapAltKullanici(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/alt-kullanicilar").WithTags("Alt kullanıcılar").RequireAuthorization();

        g.MapGet("", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Yetkili(h, o, ct, async k =>
            Results.Ok(new
            {
                limit = k.AltKullaniciLimiti,
                // Alt kullanıcıya verilebilecek yetkiler: ana kullanıcıda olanlar.
                verilebilir = new Yetkiler(k.Harita, k.AracYonetimi, k.CihazProgramlama, k.GrupYonetimi, k.Raporlar),
                kullanicilar = (await db.SorguAsync(Liste, P(k), ct)).Select(s => new
                {
                    id = Convert.ToInt32(s["ID"]),
                    kullaniciAdi = RaporYardimci.Metin(s["USERNAME"]),
                    yetkiler = new Yetkiler(Bir(s["YW"]), Bir(s["YA"]), Bir(s["YP"]), Bir(s["YG"]), Bir(s["YR"])),
                    aracSayisi = Convert.ToInt32(s["ARACSAYISI"]),
                }),
            })));

        g.MapGet("/{id:int}/araclar", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Yetkili(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(Araclari, new Dictionary<string, object?>(P(k)) { ["id"] = id }, ct)).Select(s => Convert.ToInt32(s["CLIENTID"])))));

        g.MapPost("", (AltKullaniciIstegi istek, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Yetkili(h, o, ct, async k =>
        {
            if (string.IsNullOrEmpty(istek.Sifre)) return Hata("Şifre giriniz.");
            if (await db.TekDegerAsync<int>(AltSayisi, P(k), ct) >= k.AltKullaniciLimiti)
                return Hata($"En fazla {k.AltKullaniciLimiti} alt kullanıcı açabilirsiniz.");
            var (p, hata) = await Hazirla(k, 0, istek, db, ct);
            if (hata is not null) return hata;
            var id = await db.TekDegerAsync<int>(string.Format(Ekle, AracSql(p, istek.Araclar)), p, ct);
            return Results.Ok(new { id, mesaj = "Yeni alt kullanıcı kaydedildi." });
        }));

        g.MapPut("/{id:int}", (int id, AltKullaniciIstegi istek, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Yetkili(h, o, ct, async k =>
        {
            var (p, hata) = await Hazirla(k, id, istek, db, ct);
            if (hata is not null) return hata;
            var tamam = await db.TekDegerAsync<int>(string.Format(Guncelle, AracSql(p, istek.Araclar)), p, ct);
            return tamam == 1 ? Results.Ok(new { mesaj = "Kullanıcı bilgileri güncellendi." }) : Results.NotFound(new { error = "Kullanıcı bulunamadı." });
        }));

        g.MapDelete("/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Yetkili(h, o, ct, async k =>
            await db.TekDegerAsync<int>(Sil, new Dictionary<string, object?>(P(k)) { ["id"] = id }, ct) == 1
                ? Results.Ok(new { mesaj = "Kullanıcı silindi." }) : Results.NotFound(new { error = "Kullanıcı bulunamadı." })));
    }

    private static async Task<(Dictionary<string, object?> P, IResult? Hata)> Hazirla(
        OturumKullanici k, int id, AltKullaniciIstegi istek, SqlDb db, CancellationToken ct)
    {
        var ad = istek.KullaniciAdi?.Trim() ?? "";
        if (!AdBicimi.IsMatch(ad)) return ([], Hata("Kullanıcı adı en az 3 karakter olmalı ve boşluk içermemeli."));
        var sifre = string.IsNullOrEmpty(istek.Sifre) ? null : istek.Sifre;
        if (sifre is not null && (sifre.Length < 6 || sifre.Length > 150)) return ([], Hata("Şifre en az 6 karakter olmalı."));
        if (await db.TekDegerAsync<int>(AdVarMi, new Dictionary<string, object?> { ["ad"] = ad, ["id"] = id }, ct) > 0)
            return ([], Hata("Kullanıcı adı mevcut, lütfen başka bir ad giriniz."));

        var y = istek.Yetkiler ?? new Yetkiler(false, false, false, false, false);
        // Ana kullanıcıda olmayan yetki verilemez.
        var p = new Dictionary<string, object?>(P(k))
        {
            ["id"] = id, ["ad"] = ad, ["sifre"] = sifre,
            ["yw"] = y.Harita && k.Harita ? 1 : 0,
            ["ya"] = y.AracYonetimi && k.AracYonetimi ? 1 : 0,
            ["yp"] = y.CihazProgramlama && k.CihazProgramlama ? 1 : 0,
            ["yg"] = y.GrupYonetimi && k.GrupYonetimi ? 1 : 0,
            ["yr"] = y.Raporlar && k.Raporlar ? 1 : 0,
        };
        return (p, null);
    }

    /// <summary>Araç atama SQL'i (araç yoksa yalnız eskileri siler). Kimlikler parametredir.</summary>
    private static string AracSql(Dictionary<string, object?> p, int[]? araclar)
    {
        var liste = (araclar ?? []).Where(x => x > 0).Distinct().Take(2000).ToArray();
        if (liste.Length == 0) return "DELETE FROM userclients WHERE USERID = @hedef;";
        for (var i = 0; i < liste.Length; i++) p["c" + i] = liste[i];
        return string.Format(AraclariYaz, string.Join(",", liste.Select((_, i) => "@c" + i)));
    }

    private static bool Bir(object? v) => v is not null && v is not DBNull && Convert.ToInt32(v) == 1;
    private static Dictionary<string, object?> P(OturumKullanici k) => new() { ["kullaniciId"] = k.Id };
    private static IResult Hata(string m) => Results.BadRequest(new { error = m });

    /// <summary>Eski menü koşulu: YH = 1 ve USERLIMIT &gt; 0.</summary>
    private static async Task<IResult> Yetkili(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        if (k is null) return Results.Unauthorized();
        if (!k.KullaniciAyarlari || k.AltKullaniciLimiti <= 0)
            return Results.Json(new { error = "Kullanıcı yetkilendirme yetkiniz yok." }, statusCode: StatusCodes.Status403Forbidden);
        return await is_(k);
    }
}
