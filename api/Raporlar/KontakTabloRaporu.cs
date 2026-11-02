using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// İlk/Son kontak raporu (eski: data/_raporKontakTablo.asp, _forms_raporKontakTablo.asp; istemci report_kontak_tbl).
/// Her araç için seçilen günlerde mesafe tablosundaki ilk kontak açma ve son kontak kapama saatleri
/// "SS:dd:ss - SS:dd:ss" biçiminde gün kolonlarına yazılır (bilinmeyen saat "??:??:??"). En fazla 31 gün.
/// </summary>
public sealed class KontakTabloRaporu : IRapor
{
    private const string Sorgu = """
        SELECT TARIH,
               CONCAT(ISNULL(CONVERT(varchar, ILKKONTAKAC, 108), '??:??:??'), ' - ',
                      ISNULL(CONVERT(varchar, SONKONTAKKAPAT, 108), '??:??:??')) AS ILKKONTAK
        FROM mesafe WITH (NOLOCK)
        WHERE CIHAZID = @cihazId AND TARIH BETWEEN @t1 AND @t2
        """;

    public string Kod => "report_kontak_tbl";
    public string Baslik => "İlk/Son Kontak Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var bas = istek.Baslangic!.Value.Date;
        var bit = istek.Bitis!.Value.Date;
        var gunler = Enumerable.Range(0, (int)Math.Abs((bit - bas).TotalDays) + 1).Select(i => bas.AddDays(i)).ToList();

        var kolonlar = new List<RaporKolon>
        {
            new("plaka", "Plaka"),
            new("surucu", "Sürücü"),
            new("aciklama", "Açıklama"),
            new("aractipi", "Araç Tipi"),
            new("filo", "Filo"),
        };
        kolonlar.AddRange(gunler.Select(g => new RaporKolon(GunAnahtari(g), g.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture))));

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var arac in await RaporAracKunyesi.GetirAsync(istek, db, ct))
        {
            var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
            {
                ["cihazId"] = arac.CihazId, ["t1"] = bas, ["t2"] = bit,
            }, ct);

            // Eski kodda gün başına sorgu vardı; aynı güne birden çok kayıt düşerse sonuncusu geçerliydi.
            var gunluk = new Dictionary<DateTime, string?>();
            foreach (var k in kayitlar)
                if (k["TARIH"] is DateTime t)
                    gunluk[t.Date] = k["ILKKONTAK"] as string;

            var satir = new Dictionary<string, object?>
            {
                ["plaka"] = arac.Plaka,
                ["surucu"] = arac.Surucu,
                ["aciklama"] = arac.Aciklama,
                ["aractipi"] = arac.AracTipi,
                ["filo"] = arac.Filo,
            };
            foreach (var g in gunler)
                satir[GunAnahtari(g)] = gunluk.TryGetValue(g, out var s) ? s : null;
            satirlar.Add(satir);
        }

        return new RaporSonuc(kolonlar, satirlar);
    }

    private static string GunAnahtari(DateTime g) => "g" + g.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
}
