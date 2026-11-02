using System.Globalization;
using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Yolcu raporu (eski: data/_raporYolcu.asp, _forms_raporYolcu.asp, report_yolcu()).
/// reportYolcuAsc kayıtlarından G3 kodlu ve SITUATIONS sayısal olanlarda ilk iki hane binen yolcu sayısıdır;
/// binen sayısı 0'dan büyük kayıtlar listelenir, sonda toplam satırı yazılır. En fazla 31 gün.
/// </summary>
public sealed class YolcuRaporu : IRapor
{
    private const string Sorgu = "EXEC reportYolcuAsc @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2";

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public string Kod => "report_yolcu";
    public string Baslik => "Yolcu Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

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

        var satirlar = new List<Dictionary<string, object?>>();
        var bindi = 0;

        foreach (var k in kayitlar)
        {
            var raporkodu = (k.GetValueOrDefault("RAPORKODU") as string ?? "").Trim();
            if (raporkodu != "G3") continue;

            var durum = k.GetValueOrDefault("SITUATIONS") is { } d ? Convert.ToString(d, CultureInfo.InvariantCulture) : null;
            if (durum is null || !SayiMi(durum)) continue;

            // VBScript: cint(left(SITUATIONS, 2))
            var ilkIki = durum.Length > 2 ? durum[..2] : durum;
            if (!SayiCoz(ilkIki, out var d2)) continue;
            var bindik = (int)Math.Round(d2, MidpointRounding.ToEven);
            bindi += bindik;
            if (bindik <= 0) continue;

            satirlar.Add(new()
            {
                ["plaka"] = RaporYardimci.Metin(k.GetValueOrDefault("PLAKA")) ?? arac.Plaka,
                ["tarih"] = k.GetValueOrDefault("TARIH") as DateTime?,
                ["konum"] = RaporYardimci.Metin(k.GetValueOrDefault("KONUM")),
                ["durum"] = $"{bindik} Kişi bindi",
                ["koordinat"] = RaporYardimci.Koordinat(k.GetValueOrDefault("Y"), k.GetValueOrDefault("X")),
            });
        }

        satirlar.Add(new()
        {
            ["plaka"] = null,
            ["tarih"] = null,
            ["konum"] = "TOPLAM",
            ["durum"] = $"{bindi} kişi bindi",
            ["koordinat"] = null,
        });

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("tarih", "Tarih", "tarih"),
                new("konum", "Konum"),
                new("durum", "Durum"),
                new("koordinat", "Koordinat", "koordinat"),
            ],
            satirlar,
            $"Toplam yolcu: {bindi}");
    }

    // VBScript IsNumeric yaklaşığı: hem Türkçe hem değişmez kültürde sayı olarak okunabiliyorsa sayıdır.
    private static bool SayiMi(string s) => SayiCoz(s, out _);

    private static bool SayiCoz(string s, out double d) =>
        double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, Tr, out d)
        || double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out d);
}
