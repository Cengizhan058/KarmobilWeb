using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Sürücü değerlendirme raporu (eski: data/_raporSurucuPuan.asp, _forms_raporSurucuPuan.asp, report_driverScore()).
/// seferozet tablosundan gün + sürücü + araç bazında ihlal, azami hız, rölanti, mesafe ve
/// güvenli/ekonomik sürüş skorları (eski formüllerle) hesaplanır. En fazla 31 gün.
/// </summary>
public sealed class SurucuPuanRaporu : IRapor
{
    // {araclar} yerine yalnızca parametre adları (@a0,@a1,...) yazılır; değerler parametre olarak gider.
    private const string Sorgu = """
        SELECT dbo.surucuAdi(SURUCUID) AS SURUCU, CONVERT(date, TARIHBASLA, 120) AS TARIH,
            (SELECT PLAKA FROM clients WITH (NOLOCK) WHERE clients.CIHAZID = seferozet.CIHAZID) AS PLAKA,
            SUM(ROLANTISURE) AS ROLANTI_SURE, SUM(IHLALSAY) AS IHLAL_SAY, MAX(HIZMAX) AS MAX_HIZ,
            SUM(MESAFE) / 1000 AS TOPLAM_MESAFE,
            CONVERT(BIGINT, ROUND(90 - (10 + SUM(ROLANTISURE)) / (0.01 + ((SUM(MESAFE) / 1000) / (SUM(IHLALSAY) + 0.0001))), 0)) AS EKONOMIK_SURUS,
            CONVERT(BIGINT, ROUND(90 - (10 + SUM(IHLALSAY)) / (0.0001 + ((SUM(MESAFE) / 1000) / (SUM(IHLALSAY) + 0.0001))), 0)) AS GUVENLI_SURUS
        FROM seferozet WITH (NOLOCK)
        WHERE CIHAZID IN ({araclar})
            {kimlik}
            AND TARIHBASLA BETWEEN @t1 AND @t2
            AND MESAFE > 0
            AND SURUCUID IN (SELECT KIMLIKID FROM surucukimlik WITH (NOLOCK) WHERE KIMLIKID = seferozet.SURUCUID)
        GROUP BY CONVERT(date, TARIHBASLA, 120), SURUCUID, CIHAZID
        """;

    private const string KimlikHepsi =
        "AND SURUCUID IN (SELECT KIMLIKID FROM surucukimlik WITH (NOLOCK) WHERE USERID = @kullaniciId)";

    private const string KimlikTek = "AND SURUCUID = @kimlik";

    public string Kod => "report_driverScore";
    public string Baslik => "Sürücü Değerlendirme Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    // Eski formda seçenekler kullanıcının surucukimlik kayıtlarından (KIMLIKID/ADSOYAD) doluyordu; "all" = Hepsi.
    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("kimlik_id", "Sürücü kimlik no (all = hepsi)", "metin", "all")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var kimlik = istek.Deger("kimlik_id") ?? "all";
        var hepsi = kimlik.Equals("all", StringComparison.OrdinalIgnoreCase);

        var parametreler = new Dictionary<string, object?>
        {
            ["t1"] = istek.Baslangic!.Value,
            ["t2"] = istek.Bitis!.Value,
        };
        if (hepsi) parametreler["kullaniciId"] = istek.Kullanici.Id;
        else parametreler["kimlik"] = kimlik;

        var adlar = new List<string>(istek.Araclar.Count);
        for (var i = 0; i < istek.Araclar.Count; i++)
        {
            adlar.Add("@a" + i);
            parametreler["a" + i] = istek.Araclar[i].CihazId;
        }

        var sql = Sorgu
            .Replace("{araclar}", string.Join(",", adlar))
            .Replace("{kimlik}", hepsi ? KimlikHepsi : KimlikTek);

        var kayitlar = await db.SorguAsync(sql, parametreler, ct);

        var satirlar = kayitlar.Select(k => new Dictionary<string, object?>
        {
            ["surucu"] = RaporYardimci.Metin(k.GetValueOrDefault("SURUCU")),
            ["plaka"] = RaporYardimci.Metin(k.GetValueOrDefault("PLAKA")),
            ["tarih"] = k.GetValueOrDefault("TARIH") as DateTime?,
            ["ihlal"] = k.GetValueOrDefault("IHLAL_SAY"),
            ["maxhiz"] = k.GetValueOrDefault("MAX_HIZ"),
            ["rolanti"] = k.GetValueOrDefault("ROLANTI_SURE"),
            ["mesafe"] = k.GetValueOrDefault("TOPLAM_MESAFE"),
            ["guvenli"] = k.GetValueOrDefault("GUVENLI_SURUS"),
            ["ekonomik"] = k.GetValueOrDefault("EKONOMIK_SURUS"),
        }).ToList();

        return new RaporSonuc(
            [
                new("surucu", "Sürücü"),
                new("plaka", "Plaka"),
                new("tarih", "Tarih", "tarih"),
                new("ihlal", "İhlal Sayısı", "sayi"),
                new("maxhiz", "Maksimum Hız", "sayi"),
                new("rolanti", "Rölanti Süresi", "sayi"),
                new("mesafe", "Toplam Mesafe", "km"),
                new("guvenli", "Güvenli Sürüş Skoru", "sayi"),
                new("ekonomik", "Ekonomik Sürüş Skoru", "sayi"),
            ],
            satirlar);
    }
}
