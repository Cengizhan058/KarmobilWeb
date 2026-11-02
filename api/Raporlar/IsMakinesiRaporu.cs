using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// İş makinesi çalışma raporu (eski: data/_raporwm.asp, _forms_raporwm.asp, js report_wm).
/// Eski kod _raporRolanti.asp ile aynı döngüyü kullanır (kontak açık + duruş → hareket aralıkları);
/// istemci eşik olarak sabit psure=5 gönderir. En fazla 8 gün (istemci: fark &gt; 8).
/// </summary>
public sealed class IsMakinesiRaporu : IRapor
{
    private const string Sorgu = "EXEC reportPosAsc @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2";

    // Eski istemci psure parametresini her zaman 5 gönderir; formda alan yoktur.
    private const int Esik = 5;

    public string Kod => "report_wm";
    public string Baslik => "İş Makinesi Çalışma Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 8;

    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var arac = istek.Araclar[0];

        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["cihazId"] = arac.CihazId,
            ["t1"] = RaporIstegi.SqlTarih(istek.Baslangic!.Value),
            ["t2"] = RaporIstegi.SqlTarih(istek.Bitis!.Value),
        }, ct);

        var satirlar = RolantiRaporu.Bekleme(kayitlar, Esik, arac.Plaka, koordinatli: false);
        var toplam = satirlar.Sum(s => (int)s["sure"]!);

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("baslangic", "Başlangıç", "tarih"),
                new("bitis", "Bitiş", "tarih"),
                new("konum", "Konum"),
                new("sure", "Çalışma Süresi", "sure"),
            ],
            satirlar,
            $"{satirlar.Count} çalışma, toplam {toplam / 60} sa {toplam % 60} dk");
    }
}
