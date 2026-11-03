using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Genel yakıt raporu (eski: data/_raporToplamYakit.asp). Seçilen araçlarda (boş = tümü) tarih aralığındaki
/// yakıt alımları (fms_yakit: litre, tutar), günlük mesafe tablosundan km ve 100 km'de tüketim (litre * 100 / km).
/// Yalnız yakıt alımı olan araçlar listelenir (eskisi gibi).
/// Fark: eski uç km'yi başlangıçtan ve bitişten önceki günün sayacından alıyordu (aralık bir gün kayık);
/// burada aralığın ilk günündeki ilk sayaç ile son günündeki son sayaç farkıdır, sayaç sıfırlanmışsa 0.
/// </summary>
public sealed class GenelYakitRaporu : IRapor
{
    private const string Sorgu = """
        DECLARE @g1 date = CAST(@t1 AS date), @g2 date = CAST(@t2 AS date);
        SELECT c.ID, c.PLAKA, y.LITRE, y.TUTAR, (m2.MAXM - m1.MINM) / 1000 AS KM
        FROM clients c WITH (NOLOCK)
        CROSS APPLY (SELECT SUM(LITRE) AS LITRE, SUM(FIYAT) AS TUTAR FROM fms_yakit WITH (NOLOCK)
                     WHERE CIHAZID = c.CIHAZID AND TARIH BETWEEN @t1 AND @t2) y
        OUTER APPLY (SELECT TOP 1 MINM FROM mesafe WITH (NOLOCK)
                     WHERE CIHAZID = c.CIHAZID AND TARIH BETWEEN @g1 AND @g2 ORDER BY TARIH) m1
        OUTER APPLY (SELECT TOP 1 MAXM FROM mesafe WITH (NOLOCK)
                     WHERE CIHAZID = c.CIHAZID AND TARIH BETWEEN @g1 AND @g2 ORDER BY TARIH DESC) m2
        WHERE c.ID IN ({0}) AND y.LITRE > 0
        """;

    public string Kod => "fms_toplamyakit";
    public string Baslik => "Genel Yakıt Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 0;
    public bool Gider => true;
    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var parca in istek.Araclar.Chunk(1000))
        {
            var p = new Dictionary<string, object?> { ["t1"] = istek.Baslangic, ["t2"] = istek.Bitis };
            var adlar = new string[parca.Length];
            for (var i = 0; i < parca.Length; i++) { p["a" + i] = parca[i].Id; adlar[i] = "@a" + i; }

            foreach (var k in await db.SorguAsync(string.Format(Sorgu, string.Join(",", adlar)), p, ct))
            {
                var litre = RaporYardimci.Ondalik(k["LITRE"]) ?? 0;
                var km = Math.Max(0, Math.Round(RaporYardimci.Ondalik(k["KM"]) ?? 0));
                satirlar.Add(new()
                {
                    ["plaka"] = RaporYardimci.Metin(k["PLAKA"]),
                    ["km"] = km,
                    ["litre"] = litre,
                    ["tuketim"] = km > 0 ? Math.Round(litre * 100 / km, 2) : null,
                    ["tutar"] = RaporYardimci.Ondalik(k["TUTAR"]) ?? 0,
                });
            }
        }

        satirlar.Sort((a, b) => string.Compare(a["plaka"] as string, b["plaka"] as string, new System.Globalization.CultureInfo("tr-TR"), System.Globalization.CompareOptions.IgnoreCase));
        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("km", "Toplam KM", "km"),
                new("litre", "Toplam Yakıt (Litre)", "sayi"),
                new("tuketim", "Lt / 100 km", "sayi"),
                new("tutar", "Toplam Tutar (TL)", "sayi"),
            ],
            satirlar);
    }
}
