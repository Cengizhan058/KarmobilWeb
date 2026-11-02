using System.Globalization;
using System.Security.Claims;
using Karmobil.Api.Data;

namespace Karmobil.Api.Auth;

/// <summary>
/// JWT'nin arkasındaki kullanıcıyı ve yetkilerini her istekte veritabanından çözer.
/// YACT = 0 (pasif) olan kullanıcı, jetonu hâlâ geçerli olsa bile null döner.
/// </summary>
public sealed class OturumServisi(SqlDb db)
{
    public async Task<OturumKullanici?> KullaniciAlAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var subClaim = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (subClaim is null || !int.TryParse(subClaim, out var kullaniciId))
            return null;

        var s = await db.TekSatirAsync(
            Sql.OturumKullaniciBul,
            new Dictionary<string, object?> { ["id"] = kullaniciId },
            ct);

        if (s is null || Sayi(s["YACT"]) == 0) return null;

        HaritaSiniri? sinir = null;
        if (s["MAPX1"] is not null && s["MAPY1"] is not null && s["MAPX2"] is not null && s["MAPY2"] is not null)
            sinir = new HaritaSiniri(Ondalik(s["MAPX1"]), Ondalik(s["MAPY1"]), Ondalik(s["MAPX2"]), Ondalik(s["MAPY2"]));

        return new OturumKullanici(
            Id: Convert.ToInt32(s["ID"], CultureInfo.InvariantCulture),
            KullaniciAdi: (string)s["USERNAME"]!,
            AdSoyad: s["ADI_SOYADI"] as string,
            Seviye: (s["USERLEVEL"] as string ?? "g").Trim(),
            FirmaKodu: s["FIRMAKODU"] as string,
            AracYonetimi: Sayi(s["YA"]) == 1,
            GrupYonetimi: Sayi(s["YG"]) == 1,
            Raporlar: Sayi(s["YR"]) == 1,
            CihazProgramlama: Sayi(s["YP"]) == 1,
            KullaniciAyarlari: Sayi(s["YH"]) == 1,
            Harita: Sayi(s["YW"]) == 1,
            Komut: Sayi(s["YC"]) == 1,
            Supervisor: Sayi(s["SUPERVISOR"]) == 1,
            AltKullaniciLimiti: (int)Sayi(s["USERLIMIT"]),
            AkilliBlokaj: Sayi(s["AKILLIBLOKE"]) == 1,
            HaritaSiniri: sinir,
            Tema: s["TEMA"] as string,
            Etiket: (int)Math.Clamp(Sayi(s["GORUNUM"]), 0, 3),
            AraclariGrupla: Sayi(s["ARACLARIGRUPLA"]) == 1,
            ArizalilariGizle: Sayi(s["KOPUKLARIFILTRE"]) == 1);
    }

    private static long Sayi(object? deger) =>
        deger is null ? 0 : Convert.ToInt64(deger, CultureInfo.InvariantCulture);

    private static double Ondalik(object? deger) =>
        Convert.ToDouble(deger, CultureInfo.InvariantCulture);
}
