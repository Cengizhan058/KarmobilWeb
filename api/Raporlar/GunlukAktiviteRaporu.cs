using System.Globalization;
using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Günlük aktivite raporu (eski: data/_raporGunlukAktivite2.asp, _forms_raporAktivite2.asp; istemci report_activity).
/// Başlangıçtan itibaren her gün için [gün + başlangıç saati, gün + bitiş saati] aralığında (bitiş saati başlangıçtan
/// önce/eşitse ertesi güne taşar) her araç için reportActivity yordamı çağrılır. Sonda toplam mesafe satırı. En fazla 31 gün.
/// </summary>
public sealed class GunlukAktiviteRaporu : IRapor
{
    // Eski kod: reportActivity('cihazid','tarih1','tarih2') — parametre adları bilinmediği için konumsal geçilir.
    private const string Sorgu = "EXEC reportActivity @cihazId, @t1, @t2";

    public string Kod => "report_activity";
    public string Baslik => "Günlük Aktivite Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    public IReadOnlyList<RaporParametre> Parametreler { get; } = [];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        // Eski kod saniye hassasiyetinde çalışıyordu (tarihFormati).
        var bas = SaniyeyeKirp(istek.Baslangic!.Value);
        var bit = SaniyeyeKirp(istek.Bitis!.Value);
        var sonSaat = bit.TimeOfDay;
        var gunFarki = Math.Abs((bit - bas).TotalMinutes) / 60 / 24;   // eski f_sonuc (kesirli olabilir)

        var araclar = await RaporAracKunyesi.GetirAsync(istek, db, ct);
        var satirlar = new List<Dictionary<string, object?>>();
        long genelMesafe = 0;

        for (var i = 0; i <= gunFarki; i++)
        {
            var tarih = bas.AddDays(i);
            var ilk = tarih;
            var son = tarih.Date + sonSaat;
            if (son <= tarih) son = tarih.Date.AddDays(1) + sonSaat;

            foreach (var arac in araclar)
            {
                var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
                {
                    ["cihazId"] = arac.CihazId,
                    ["t1"] = RaporIstegi.SqlTarih(ilk),
                    ["t2"] = RaporIstegi.SqlTarih(son),
                }, ct);

                foreach (var k in kayitlar)
                {
                    // Eski: CLng(mesafe) — banker yuvarlaması.
                    var mesafe = RaporYardimci.Ondalik(k.GetValueOrDefault("mesafe")) is { } m ? (long)Math.Round(m) : 0L;
                    genelMesafe += mesafe;

                    satirlar.Add(new()
                    {
                        ["tarih"] = tarih.Date,
                        ["plaka"] = arac.Plaka,
                        ["filo"] = arac.Filo,
                        ["ilkhareket"] = TarihDegeri(k.GetValueOrDefault("ilkhareket")),
                        ["sonhareket"] = TarihDegeri(k.GetValueOrDefault("sonhareket")),
                        ["maxhiz"] = RaporYardimci.Ondalik(k.GetValueOrDefault("maxhiz")),
                        ["ihlal"] = RaporYardimci.Ondalik(k.GetValueOrDefault("ihlalsay")),
                        ["rolanti"] = RaporYardimci.Ondalik(k.GetValueOrDefault("ROLANTI_SURE")),
                        ["hareket"] = RaporYardimci.Ondalik(k.GetValueOrDefault("HAREKET_SURE")),
                        ["mesafe"] = mesafe,
                    });
                }
            }
        }

        satirlar.Add(new() { ["plaka"] = "Toplam", ["mesafe"] = genelMesafe });

        return new RaporSonuc(
            [
                new("tarih", "Tarih", "tarih"),
                new("plaka", "Plaka"),
                new("filo", "Filo"),
                new("ilkhareket", "İlk hareket", "tarih"),
                new("sonhareket", "Son hareket", "tarih"),
                new("maxhiz", "Maksimum Hız (km/h)", "sayi"),
                new("ihlal", "İhlal Sayısı", "sayi"),
                new("rolanti", "Rölanti Süresi (dk)", "sure"),
                new("hareket", "Hareket Süresi (dk)", "sure"),
                new("mesafe", "Toplam Mesafe (km)", "km"),
            ],
            satirlar,
            $"Toplam mesafe: {genelMesafe} km");
    }

    private static DateTime SaniyeyeKirp(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, d.Kind);

    private static DateTime? TarihDegeri(object? v) => v switch
    {
        DateTime d => d,
        string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) => d,
        _ => null,
    };
}
