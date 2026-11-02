using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Aylık KM raporu / mesafe tablosu (eski: data/_raporMesafeTablo.asp, _forms_raporMesafeTablo.asp; istemci report_distance_tbl).
/// Her araç için seçilen günlerin mesafe tablosundaki (mesafe) günlük km değerleri yan yana kolonlarda gösterilir.
/// kmType: toplam (MAXM-MINM), mici (MESAI, negatifse 0), mdisi (toplam - mesai içi). Değerler metre; 1000 m altı 0 yazılır.
/// En fazla 31 gün.
/// </summary>
public sealed class MesafeTabloRaporu : IRapor
{
    private const string Sorgu = """
        SELECT TARIH,
               (MAXM - MINM) AS TOPLAM,
               (CASE WHEN MESAI < 0 THEN 0 ELSE MESAI END) AS MICI,
               (MAXM - MINM) - (CASE WHEN MESAI < 0 THEN 0 ELSE MESAI END) AS MDISI
        FROM mesafe WITH (NOLOCK)
        WHERE CIHAZID = @cihazId AND TARIH BETWEEN @t1 AND @t2
        """;

    public string Kod => "report_distance_tbl";
    public string Baslik => "Aylık KM Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("kmType", "Mesafe", "secim", "toplam",
        [
            new RaporSecenek("toplam", "Toplam"),
            new RaporSecenek("mici", "Mesai İçi"),
            new RaporSecenek("mdisi", "Mesai Dışı"),
        ]),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var kolon = istek.Deger("kmType") switch { "mici" => "MICI", "mdisi" => "MDISI", _ => "TOPLAM" };
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
        kolonlar.AddRange(gunler.Select(g => new RaporKolon(GunAnahtari(g), g.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture), "km")));
        kolonlar.Add(new("toplam", "Toplam Km", "km"));

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var arac in await RaporAracKunyesi.GetirAsync(istek, db, ct))
        {
            var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
            {
                ["cihazId"] = arac.CihazId, ["t1"] = bas, ["t2"] = bit,
            }, ct);

            // Eski kodda gün başına sorgu vardı; aynı güne birden çok kayıt düşerse sonuncusu geçerliydi.
            var gunluk = new Dictionary<DateTime, double>();
            foreach (var k in kayitlar)
                if (k["TARIH"] is DateTime t)
                    gunluk[t.Date] = RaporYardimci.Ondalik(k[kolon]) ?? 0;

            var satir = new Dictionary<string, object?>
            {
                ["plaka"] = arac.Plaka,
                ["surucu"] = arac.Surucu,
                ["aciklama"] = arac.Aciklama,
                ["aractipi"] = arac.AracTipi,
                ["filo"] = arac.Filo,
            };
            double genel = 0;
            foreach (var g in gunler)
            {
                var metre = gunluk.TryGetValue(g, out var m) ? m : 0;
                genel += metre;
                satir[GunAnahtari(g)] = metre >= 1000 ? metre / 1000 : 0;
            }
            satir["toplam"] = genel >= 1000 ? genel / 1000 : 0;
            satirlar.Add(satir);
        }

        return new RaporSonuc(kolonlar, satirlar);
    }

    private static string GunAnahtari(DateTime g) => "g" + g.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Mesafe/kontak/aktivite tablolarında araç künyesi (plaka, sürücü, açıklama, araç tipi, filo).</summary>
internal sealed record RaporAracKunye(int Id, string CihazId, string? Plaka, string? AracTipi, string? Surucu, string? Aciklama, string? Filo);

/// <summary>
/// Eski raporların araç listesi sorgusu (clients + dbo.filoBul; KOPUKLARIFILTRE açıksa dbo.aktifMi), ID sırasıyla.
/// Araç kimlikleri @a0,@a1,... parametreleriyle verilir.
/// </summary>
internal static class RaporAracKunyesi
{
    private const string Sorgu = """
        SELECT c.ID, c.CIHAZID, c.PLAKA, c.ARACTIPI, c.SOFOR, c.EHLIYETNO,
               dbo.filoBul(@kullaniciId, c.ID) AS FILO
        FROM clients c WITH (NOLOCK)
        WHERE c.STATUS = 1
          AND (@kopuk = 0 OR dbo.aktifMi(c.KONTROLTARIH) = 1)
          AND c.ID IN ({0})
        ORDER BY c.ID
        """;

    public static async Task<List<RaporAracKunye>> GetirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var sonuc = new List<RaporAracKunye>();
        foreach (var parca in istek.Araclar.OrderBy(a => a.Id).Chunk(1000))
        {
            var p = new Dictionary<string, object?>
            {
                ["kullaniciId"] = istek.Kullanici.Id.ToString(),
                ["kopuk"] = istek.Kullanici.ArizalilariGizle ? 1 : 0,
            };
            var adlar = new string[parca.Length];
            for (var i = 0; i < parca.Length; i++)
            {
                p["a" + i] = parca[i].Id;
                adlar[i] = "@a" + i;
            }

            var kayitlar = await db.SorguAsync(string.Format(Sorgu, string.Join(",", adlar)), p, ct);
            sonuc.AddRange(kayitlar.Select(k => new RaporAracKunye(
                Convert.ToInt32(k["ID"]),
                (k["CIHAZID"] as string ?? "").Trim(),
                RaporYardimci.Metin(k["PLAKA"]),
                RaporYardimci.Metin(k["ARACTIPI"]),
                RaporYardimci.Metin(k["SOFOR"]),
                RaporYardimci.Metin(k["EHLIYETNO"]),
                RaporYardimci.Metin(k["FILO"]))));
        }
        return sonuc;
    }
}
