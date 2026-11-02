using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Araç bakım raporu (eski: data/_raporBakim.asp). Kullanıcının araçlarında motor / lastik bakımına kalan km:
/// round((MOTOR_KM - SKMESAFE) / 1000), negatifse 0. Yalnızca MOTOR_KM ya da LASTIK_KM &gt; 0 olan araçlar.
/// KOPUKLARIFILTRE açıksa yalnızca aktif (dbo.aktifMi) araçlar.
/// </summary>
public sealed class BakimRaporu : IRapor
{
    private const string Sorgu = """
        SELECT c.ID, c.PLAKA, c.SKMESAFE, c.MOTOR_KM, c.LASTIK_KM
        FROM clients c WITH (NOLOCK), userclients u WITH (NOLOCK)
        WHERE c.STATUS = 1 AND u.USERID = @kullaniciId AND c.ID = u.CLIENTID
        ORDER BY PLAKA, c.ID
        """;

    private const string SorguAktifler = """
        SELECT c.ID, c.PLAKA, c.SKMESAFE, c.MOTOR_KM, c.LASTIK_KM
        FROM clients c WITH (NOLOCK), userclients u WITH (NOLOCK)
        WHERE c.STATUS = 1 AND dbo.aktifMi(c.KONTROLTARIH) = 1 AND u.USERID = @kullaniciId AND c.ID = u.CLIENTID
        ORDER BY PLAKA, c.ID
        """;

    public string Kod => "report_bakim";
    public string Baslik => "Araç Bakım Raporu";
    public AracSecimi Arac => AracSecimi.Yok;
    public TarihSecimi Tarih => TarihSecimi.Yok;
    public int AzamiGun => 0;
    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var kayitlar = await db.SorguAsync(
            istek.Kullanici.ArizalilariGizle ? SorguAktifler : Sorgu,
            new Dictionary<string, object?> { ["kullaniciId"] = istek.Kullanici.Id.ToString() }, ct);

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var k in kayitlar)
        {
            var mesafe = RaporYardimci.Ondalik(k["SKMESAFE"]);
            var motor = RaporYardimci.Ondalik(k["MOTOR_KM"]);
            var lastik = RaporYardimci.Ondalik(k["LASTIK_KM"]);
            if (!(motor > 0 || lastik > 0)) continue;

            satirlar.Add(new()
            {
                ["kayitno"] = k["ID"],
                ["plaka"] = RaporYardimci.Metin(k["PLAKA"]),
                ["motor"] = Kalan(motor, mesafe),
                ["lastik"] = Kalan(lastik, mesafe),
            });
        }

        return new RaporSonuc(
            [
                new("kayitno", "Kayıt No", "sayi"),
                new("plaka", "Plaka"),
                new("motor", "Motor Bakıma Kalan KM", "km"),
                new("lastik", "Lastik Bakıma Kalan KM", "km"),
            ],
            satirlar);
    }

    /// <summary>VBScript Round (yarıyı çifte) ile km; NULL girdide boş (eski kodda Null yayılırdı).</summary>
    private static double? Kalan(double? hedef, double? mesafe)
    {
        if (hedef is null || mesafe is null) return null;
        var kalan = Math.Round((hedef.Value - mesafe.Value) / 1000, MidpointRounding.ToEven);
        return kalan < 0 ? 0 : kalan;
    }
}
