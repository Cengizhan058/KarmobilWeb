using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Gider esnek raporu (eski: data/_raporFMSEsnek.asp, _forms_fmsEsnek.asp). Seçilen araçlarda (boş = tümü)
/// seçilen masraf türlerinin araç başına toplamı ve genel toplam; fms_toplammasraflar görünümünden.
/// Fark: eski formdaki tutar süzgeci (&lt; = &gt;) uçta hesaplanıp sorguya hiç eklenmiyordu; burada her gider
/// kaydına uygulanır. Seçilen türlerde hiç gideri olmayan araç listelenmez (eskisi sıfırlarla gösteriyordu).
/// </summary>
public sealed class GiderEsnekRaporu : IRapor
{
    private const string Sorgu = """
        SELECT r.CIHAZID, r.RPTABLE, SUM(r.FIYAT) AS TOPLAM
        FROM fms_toplammasraflar r WITH (NOLOCK)
        WHERE r.TARIH BETWEEN @t1 AND @t2
          AND r.CIHAZID IN ({0})
          AND r.RPTABLE IN ({1})
          {2}
        GROUP BY r.CIHAZID, r.RPTABLE
        """;

    private const string FiyatKucuk = "AND r.FIYAT < @fiyat";
    private const string FiyatEsit = "AND r.FIYAT = @fiyat";
    private const string FiyatBuyuk = "AND r.FIYAT > @fiyat";

    /// <summary>Onay kutuları, eski form sırası ve varsayılanlarıyla: parametre, görünümdeki RPTABLE, başlık.</summary>
    private static readonly (string Ad, string Tablo, string Baslik, bool Varsayilan)[] Turler =
    [
        ("fields_ceza", "fms_ceza", "Ceza", false),
        ("fields_kaza", "fms_kaza", "Kaza", false),
        ("fields_lastik", "fms_lastik", "Lastik", true),
        ("fields_mtv", "fms_mtv", "MTV", false),
        ("fields_periyodik", "fms_periyodik", "Periyodik", false),
        ("fields_muayene", "fms_muayene", "Muayene", false),
        ("fields_ogs", "fms_ogs", "OGS/HGS", true),
        ("fields_sigorta", "fms_sigorta", "Sigorta", false),
        ("fields_yakit", "fms_yakit", "Yakıt", true),
        ("fields_diger", "fms_digermasraf", "Diğer", false),
    ];

    public string Kod => "fms_custom";
    public string Baslik => "Gider Esnek Rapor";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 0;
    public bool Gider => true;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("fiyat2", "Tutar (TL)", "secim", "0", [new("0", "<"), new("1", "="), new("2", ">")]),
        new RaporParametre("fiyat", "Tutar", "sayi"),
        .. Turler.Select(t => new RaporParametre(t.Ad, t.Baslik, "onay", t.Varsayilan ? "1" : "0")),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var secili = Turler.Where(t => istek.Onay(t.Ad)).ToList();
        if (secili.Count == 0) throw new RaporHatasi("En az bir masraf türü seçmelisiniz.");

        var p = new Dictionary<string, object?> { ["t1"] = istek.Baslangic, ["t2"] = istek.Bitis };
        var tablolar = new string[secili.Count];
        for (var i = 0; i < secili.Count; i++) { p["r" + i] = secili[i].Tablo; tablolar[i] = "@r" + i; }

        var fiyatKosulu = "";
        if (RaporYardimci.Ondalik(istek.Deger("fiyat")) is { } fiyat)
        {
            p["fiyat"] = fiyat;
            fiyatKosulu = istek.Deger("fiyat2") switch { "1" => FiyatEsit, "2" => FiyatBuyuk, _ => FiyatKucuk };
        }

        // Araç başına { tablo → toplam }.
        var toplamlar = new Dictionary<string, Dictionary<string, double>>();
        foreach (var parca in istek.Araclar.Chunk(1000))
        {
            var pp = new Dictionary<string, object?>(p);
            var adlar = new string[parca.Length];
            for (var i = 0; i < parca.Length; i++) { pp["c" + i] = parca[i].CihazId; adlar[i] = "@c" + i; }

            var kayitlar = await db.SorguAsync(string.Format(Sorgu, string.Join(",", adlar), string.Join(",", tablolar), fiyatKosulu), pp, ct);
            foreach (var k in kayitlar)
            {
                var cihaz = (k["CIHAZID"] as string ?? "").Trim();
                if (!toplamlar.TryGetValue(cihaz, out var t)) toplamlar[cihaz] = t = [];
                t[(string)k["RPTABLE"]!] = RaporYardimci.Ondalik(k["TOPLAM"]) ?? 0;
            }
        }

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var a in istek.Araclar.OrderBy(a => a.Plaka, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), true)))
        {
            if (!toplamlar.TryGetValue(a.CihazId, out var t)) continue;
            var satir = new Dictionary<string, object?> { ["plaka"] = a.Plaka };
            double toplam = 0;
            foreach (var s in secili)
            {
                var deger = t.GetValueOrDefault(s.Tablo);
                satir[s.Tablo] = deger;
                toplam += deger;
            }
            satir["toplam"] = toplam;
            satirlar.Add(satir);
        }

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                .. secili.Select(s => new RaporKolon(s.Tablo, s.Baslik, "sayi")),
                new("toplam", "Toplam", "sayi"),
            ],
            satirlar,
            satirlar.Count > 0 ? $"{satirlar.Count} araç, genel toplam {satirlar.Sum(s => (double)s["toplam"]!).ToString("N2", new System.Globalization.CultureInfo("tr-TR"))} TL" : null);
    }
}
