using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Park raporu (eski: data/_raporPark.asp, _forms_raporPark.asp).
/// reportPosAscP yordamının kayıtlarında hareket (hız &gt; 5) ↔ duruş (VD ya da hız ≤ 5) geçişlerinden
/// park aralıkları çıkarılır; "Park süresi (dk)" eşiğini geçenler listelenir. En fazla 8 gün.
/// </summary>
public sealed class ParkRaporu : IRapor
{
    private const string Sorgu = "EXEC reportPosAscP @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2, @uid = @kullaniciId";

    public string Kod => "report_park";
    public string Baslik => "Park Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 8;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("psure", "Park süresi (dk)", "sayi", "30")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var arac = istek.Araclar[0];
        var esik = istek.Sayi("psure", 30);
        var t1 = RaporIstegi.SqlTarih(istek.Baslangic!.Value);
        var t2 = RaporIstegi.SqlTarih(istek.Bitis!.Value);

        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["cihazId"] = arac.CihazId, ["t1"] = t1, ["t2"] = t2, ["kullaniciId"] = istek.Kullanici.Id.ToString(),
        }, ct);

        var satirlar = new List<Dictionary<string, object?>>();
        string? onceki = null;
        DateTime parkBaslangic = istek.Baslangic.Value, hareket = istek.Baslangic.Value;

        foreach (var k in kayitlar)
        {
            var hiz = Convert.ToDouble(k["HIZ"] ?? 0);
            var kod = (k["RAPORKODU"] as string ?? "").Trim() == "VD" || hiz <= 5 ? "VD" : "VZ";
            var tarih = (DateTime)k["TARIH"]!;
            onceki ??= kod;

            if (kod == "VD" && onceki == "VZ") parkBaslangic = tarih;
            if (kod == "VZ" && onceki == "VD") hareket = tarih;

            if (onceki == "VD" && kod == "VZ")
            {
                var dakika = (int)(hareket - parkBaslangic).TotalMinutes;
                if (dakika >= esik)
                {
                    satirlar.Add(new()
                    {
                        ["plaka"] = arac.Plaka,
                        ["park"] = parkBaslangic,
                        ["hareket"] = hareket,
                        ["konum"] = RaporYardimci.Metin(k["KONUMX"]),
                        ["sure"] = dakika,
                        ["koordinat"] = RaporYardimci.Koordinat(k["Y"], k["X"]),
                    });
                }
            }
            onceki = kod;
        }

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("park", "Park zamanı", "tarih"),
                new("hareket", "Hareket zamanı", "tarih"),
                new("konum", "Konum"),
                new("sure", "Süre", "sure"),
                new("koordinat", "Koordinat", "koordinat"),
            ],
            satirlar,
            $"{satirlar.Count} park ({esik} dk ve üzeri)");
    }
}
