using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Havuz verimlilik raporu (eski: data/_raporHavuz.asp, _forms_raporHavuz.asp, report_havuz()).
/// Her araç için reportActivity yordamından rölanti/hareket süreleri ve mesafe alınır;
/// kullanım süresi ve oranlar eski formüllerle hesaplanır. Araçlar eskisi gibi clients.ID sırasıyla işlenir.
/// En fazla 7 gün.
/// </summary>
public sealed class HavuzRaporu : IRapor
{
    // {araclar} yerine yalnızca parametre adları (@a0,@a1,...) yazılır; değerler parametre olarak gider.
    private const string FiloSorgu = "SELECT c.ID, dbo.filoBul(@kullaniciId, c.ID) AS FILO FROM clients c WITH (NOLOCK) WHERE c.ID IN ({araclar})";

    // Eski kod: reportActivity('imei','tarih1','tarih2') — parametreler sırayla.
    private const string Sorgu = "EXEC reportActivity @cihazId, @t1, @t2";

    public string Kod => "report_havuz";
    public string Baslik => "Havuz Verimlilik Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 7;

    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var bas = istek.Baslangic!.Value;
        var bit = istek.Bitis!.Value;
        var araclar = istek.Araclar.OrderBy(a => a.Id).ToList();

        // Filo adları (dbo.filoBul) tek sorguda.
        var filoParam = new Dictionary<string, object?> { ["kullaniciId"] = istek.Kullanici.Id.ToString() };
        var adlar = new List<string>(araclar.Count);
        for (var i = 0; i < araclar.Count; i++)
        {
            adlar.Add("@a" + i);
            filoParam["a" + i] = araclar[i].Id;
        }
        var filolar = (await db.SorguAsync(FiloSorgu.Replace("{araclar}", string.Join(",", adlar)), filoParam, ct))
            .ToDictionary(s => Convert.ToInt32(s["ID"]), s => RaporYardimci.Metin(s.GetValueOrDefault("FILO")));

        var t1 = RaporIstegi.SqlTarih(bas);
        var t2 = RaporIstegi.SqlTarih(bit);
        var satirlar = new List<Dictionary<string, object?>>();

        foreach (var arac in araclar)
        {
            var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
            {
                ["cihazId"] = arac.CihazId, ["t1"] = t1, ["t2"] = t2,
            }, ct);

            foreach (var k in kayitlar)
            {
                var toplamMesafe = Convert.ToInt64(RaporYardimci.Ondalik(k.GetValueOrDefault("mesafe")) ?? 0); // CLng
                var rSure = RaporYardimci.Ondalik(k.GetValueOrDefault("ROLANTI_SURE")) ?? 0;
                var hSure = RaporYardimci.Ondalik(k.GetValueOrDefault("HAREKET_SURE")) ?? 0;
                var tSure = rSure + hSure;

                // Eski kodda tSure/hOran1 sıfırsa sayfa hata veriyordu; burada 0 yazılır.
                var rOran1 = tSure != 0 ? Math.Round(rSure / tSure * 100, 2) : 0;
                var hOran1 = Math.Round(100 - rOran1, 2);
                var rOran2 = hOran1 != 0 ? Math.Round(rOran1 / hOran1 * 100, 2) : 0;

                satirlar.Add(new()
                {
                    ["plaka"] = arac.Plaka,
                    ["filo"] = filolar.GetValueOrDefault(arac.Id),
                    ["baslangic"] = bas,
                    ["bitis"] = bit,
                    ["kullanim"] = tSure,
                    ["rolanti"] = rSure,
                    ["hareket"] = hSure,
                    ["mesafe"] = toplamMesafe,
                    ["rolantioran"] = rOran1,
                    ["kullanimoran"] = hOran1,
                    ["calismarolantioran"] = rOran2,
                });
            }
        }

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("filo", "Filo"),
                new("baslangic", "Başlangıç", "tarih"),
                new("bitis", "Bitiş", "tarih"),
                new("kullanim", "Kullanım Süresi", "sure"),
                new("rolanti", "Rölanti Süresi", "sure"),
                new("hareket", "Hareket Süresi", "sure"),
                new("mesafe", "Toplam Mesafe (km)", "km"),
                new("rolantioran", "Kullanım Süresindeki Rölanti Oranı", "sayi"),
                new("kullanimoran", "Çalışma Süresindeki Kullanım Oranı", "sayi"),
                new("calismarolantioran", "Çalışma Süresindeki Rölanti Oranı", "sayi"),
            ],
            satirlar);
    }
}
