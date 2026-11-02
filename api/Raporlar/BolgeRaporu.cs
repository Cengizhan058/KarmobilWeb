using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Bölge raporu (eski: data/_raporBolgeNew.asp, _forms_raporBolge.asp, js report_bolge).
/// Seçilen her araç × her bölge için reportRegion (cihazid, bölge ID, tarih1, tarih2) çalıştırılır;
/// bölgeye giriş/çıkış ve bölgede kalma süresi listelenir.
/// Bölge: kullanıcının regions kayıtlarından biri (ID ya da adı) veya "all" (tüm bölgeler).
/// Sınırlar (eski istemci): tek araç + tek bölge en fazla 10 gün; çok araç ya da tüm bölgeler en fazla 1 gün;
/// çok araç seçildiğinde yalnızca tek bölge.
/// </summary>
public sealed class BolgeRaporu : IRapor
{
    private const string TumBolgeler =
        "SELECT ID FROM regions WITH (NOLOCK) WHERE USERID = @kullaniciId";

    private const string TekBolge =
        "SELECT ID FROM regions WITH (NOLOCK) WHERE USERID = @kullaniciId AND (CONVERT(varchar(20), ID) = @bolge OR REGION_NAME = @bolge)";

    private const string Sorgu = "EXEC reportRegion @cihazId, @bolgeId, @t1, @t2";

    public string Kod => "report_region";
    public string Baslik => "Bölge Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 10;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("bolge", "Bölge (ID, adı ya da all)", "metin")];

    private static readonly IReadOnlyList<RaporKolon> Kolonlar =
    [
        new("plaka", "Plaka"),
        new("bolge", "Bölge"),
        new("giris", "Giriş Tarihi", "tarih"),
        new("giriskonum", "Giriş Konumu"),
        new("cikis", "Çıkış Tarihi", "tarih"),
        new("cikiskonum", "Çıkış Konumu"),
        new("sure", "Bölgede Kalma Süresi", "sure"),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var bolge = istek.Deger("bolge");
        if (bolge is null || bolge.Equals("sec", StringComparison.OrdinalIgnoreCase))
            return new RaporSonuc(Kolonlar, [], "Bölge seçiniz.");

        var tumBolgeler = bolge.Equals("all", StringComparison.OrdinalIgnoreCase)
                          || bolge.Equals("hepsi", StringComparison.OrdinalIgnoreCase);
        var cokArac = istek.Araclar.Count > 1;
        var gun = (istek.Bitis!.Value - istek.Baslangic!.Value).TotalDays;

        if (cokArac && tumBolgeler)
            return new RaporSonuc(Kolonlar, [], "Tüm araçlar seçildiğinde maksimum 1 günlük tarih aralığında ve sadece 1 bölge için sorgulama yapabilirsiniz.");
        if ((cokArac || tumBolgeler) && gun > 1)
            return new RaporSonuc(Kolonlar, [], "Tarih aralığı 1 günden fazla olan sorgulamalar için sadece 1 araç ve 1 bölge seçmelisiniz.");

        // Bölgeler yalnızca kullanıcının kendi regions kayıtlarından.
        var bolgeler = tumBolgeler
            ? await db.SorguAsync(TumBolgeler, new Dictionary<string, object?> { ["kullaniciId"] = istek.Kullanici.Id }, ct)
            : await db.SorguAsync(TekBolge, new Dictionary<string, object?> { ["kullaniciId"] = istek.Kullanici.Id, ["bolge"] = bolge }, ct);
        if (bolgeler.Count == 0)
            return new RaporSonuc(Kolonlar, [], "Bölge bulunamadı.");

        var t1 = RaporIstegi.SqlTarih(istek.Baslangic.Value);
        var t2 = RaporIstegi.SqlTarih(istek.Bitis.Value);
        var satirlar = new List<Dictionary<string, object?>>();

        foreach (var arac in istek.Araclar)
        {
            foreach (var b in bolgeler)
            {
                var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
                {
                    ["cihazId"] = arac.CihazId, ["bolgeId"] = b["ID"], ["t1"] = t1, ["t2"] = t2,
                }, ct);

                foreach (var k in kayitlar)
                {
                    // SURE saniye; eski kod 0 ve altını "-" gösterir.
                    var saniye = RaporYardimci.Ondalik(k["SURE"]);
                    satirlar.Add(new()
                    {
                        ["plaka"] = RaporYardimci.Metin(k["PLAKA"]) ?? arac.Plaka,
                        ["bolge"] = RaporYardimci.Metin(k["BOLGE"]),
                        ["giris"] = k["GIRIS_TARIHI"] as DateTime?,
                        ["giriskonum"] = RaporYardimci.Metin(k["GIRIS_KONUMU"]),
                        ["cikis"] = k["CIKIS_TARIHI"] as DateTime?,
                        ["cikiskonum"] = RaporYardimci.Metin(k["CIKIS_KONUMU"]),
                        ["sure"] = saniye is > 0 ? Math.Round(saniye.Value / 60.0, 2) : null,
                    });
                }
            }
        }

        return new RaporSonuc(Kolonlar, satirlar, $"{satirlar.Count} bölge girişi");
    }
}
