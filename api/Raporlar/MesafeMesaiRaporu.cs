using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Mesai KM raporu (eski: data/_raporMesafeMesai.asp, _forms_raporMesafeMesai.asp; istemci report_workhours).
/// Seçilen günler için mesafe tablosundan araç başına toplam (MAXM-MINM) ve mesai içi (MESAI) metre toplanır,
/// km'ye çevrilip yuvarlanır; mesai dışı = toplam - mesai içi. En fazla 31 gün.
/// </summary>
public sealed class MesafeMesaiRaporu : IRapor
{
    private const string Sorgu = """
        SELECT SUM(MAXM - MINM) AS MESAFE, SUM(MESAI) AS MICIKM
        FROM mesafe WITH (NOLOCK)
        WHERE TARIH BETWEEN @t1 AND @t2 AND CIHAZID = @cihazId
        """;

    public string Kod => "report_workhours";
    public string Baslik => "Mesai KM Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var bas = istek.Baslangic!.Value.Date;
        var bit = istek.Bitis!.Value.Date;

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var arac in await RaporAracKunyesi.GetirAsync(istek, db, ct))
        {
            var k = await db.TekSatirAsync(Sorgu, new Dictionary<string, object?>
            {
                ["t1"] = bas, ["t2"] = bit, ["cihazId"] = arac.CihazId,
            }, ct);

            // VBScript round() ile aynı: banker yuvarlaması (Math.Round varsayılanı ToEven).
            double toplamKm = 0, miciKm = 0;
            if (k is not null)
            {
                if (RaporYardimci.Ondalik(k["MESAFE"]) is { } m) toplamKm = Math.Round(m / 1000);
                if (RaporYardimci.Ondalik(k["MICIKM"]) is { } mi) miciKm = Math.Round(mi / 1000);
            }

            satirlar.Add(new()
            {
                ["plaka"] = arac.Plaka,
                ["surucu"] = arac.Surucu,
                ["aciklama"] = arac.Aciklama,
                ["aractipi"] = arac.AracTipi,
                ["filo"] = arac.Filo,
                ["mici"] = miciKm,
                ["mdisi"] = toplamKm - miciKm,
                ["toplam"] = toplamKm,
            });
        }

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("surucu", "Sürücü"),
                new("aciklama", "Açıklama"),
                new("aractipi", "Araç Tipi"),
                new("filo", "Filo"),
                new("mici", "Mesai İçi Km", "km"),
                new("mdisi", "Mesai Dışı Km", "km"),
                new("toplam", "Toplam Km", "km"),
            ],
            satirlar);
    }
}
