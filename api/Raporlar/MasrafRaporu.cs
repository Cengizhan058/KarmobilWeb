using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Masraf raporları (eski: data/_raporMasraflar.asp). Tek aracın tarih aralığındaki gider kayıtları,
/// fms_toplammasraflar görünümünden (on gider tablosunun birleşimi); masraf türü "Hepsi" ya da tek tür.
/// Eski uçtaki tür başına sorgular görünümdeki ifadelerin aynısıydı, burada hepsi görünümden okunur.
/// </summary>
public sealed class MasrafRaporu : IRapor
{
    private const string Sorgu = """
        SELECT r.TARIH, c.PLAKA, r.RPTURU, r.URUN, r.ACIKLAMA, r.FIYAT
        FROM fms_toplammasraflar r WITH (NOLOCK), clients c WITH (NOLOCK)
        WHERE c.CIHAZID = r.CIHAZID AND c.FMS = 1 AND r.CIHAZID = @cihazId
          AND r.TARIH BETWEEN @t1 AND @t2
          AND (@tablo = '' OR r.RPTABLE = @tablo)
        ORDER BY r.TARIH DESC
        """;

    /// <summary>Eski formdaki seçenek değerleri → görünümdeki RPTABLE.</summary>
    private static readonly (string Deger, string Ad, string Tablo)[] Turler =
    [
        ("All", "Hepsi", ""),
        ("Ceza", "Ceza", "fms_ceza"),
        ("Diger", "Diğer", "fms_digermasraf"),
        ("Kaza", "Kaza", "fms_kaza"),
        ("Lastik", "Lastik", "fms_lastik"),
        ("MTV", "MTV", "fms_mtv"),
        ("Muayene", "Muayene", "fms_muayene"),
        ("OGSHGS", "OGS/HGS", "fms_ogs"),
        ("Periyodik", "Periyodik Bakım", "fms_periyodik"),
        ("Sigorta", "Sigorta", "fms_sigorta"),
        ("Yakit", "Yakıt", "fms_yakit"),
    ];

    public string Kod => "fms_masrafrapor";
    public string Baslik => "Masraf Raporları";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 0;
    public bool Gider => true;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("rprturu", "Masraf türü", "secim", "All", Turler.Select(t => new RaporSecenek(t.Deger, t.Ad)).ToList()),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var tablo = Turler.FirstOrDefault(t => t.Deger.Equals(istek.Deger("rprturu") ?? "All", StringComparison.OrdinalIgnoreCase)).Tablo ?? "";
        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["cihazId"] = istek.Araclar[0].CihazId,
            ["t1"] = istek.Baslangic,
            ["t2"] = istek.Bitis,
            ["tablo"] = tablo,
        }, ct);

        double toplam = 0;
        var satirlar = kayitlar.Select(k =>
        {
            var fiyat = RaporYardimci.Ondalik(k["FIYAT"]);
            toplam += fiyat ?? 0;
            return new Dictionary<string, object?>
            {
                ["tarih"] = k["TARIH"],
                ["plaka"] = RaporYardimci.Metin(k["PLAKA"]),
                ["tur"] = RaporYardimci.Metin(k["RPTURU"]),
                ["urun"] = RaporYardimci.Metin(k["URUN"]),
                ["aciklama"] = RaporYardimci.Metin(k["ACIKLAMA"]),
                ["fiyat"] = fiyat,
            };
        }).ToList();

        return new RaporSonuc(
            [
                new("tarih", "Tarih", "tarih"),
                new("plaka", "Plaka"),
                new("tur", "Türü"),
                new("urun", "Harcama Konusu"),
                new("aciklama", "Açıklama"),
                new("fiyat", "Fiyatı (TL)", "sayi"),
            ],
            satirlar,
            satirlar.Count > 0 ? $"{satirlar.Count} kayıt, toplam {toplam.ToString("N2", Turkce)} TL" : null);
    }

    private static readonly System.Globalization.CultureInfo Turkce = new("tr-TR");
}
