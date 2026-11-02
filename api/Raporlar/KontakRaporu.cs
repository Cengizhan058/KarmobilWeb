using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Sefer özeti (kontak) raporu (eski: data/_raporKontak.asp, _forms_raporKontak.asp, report_ignition()).
/// reportPosAsc kayıtlarında G2 (kontak açıldı) → G1 (kontak kapandı) çiftleri sefer sayılır;
/// VI kayıtlarındaki kimlikten sürücü adı bulunur. 1 km ve üzeri seferler listelenir,
/// toplam süre/mesafe ise (eskisi gibi) tüm seferleri kapsar. En fazla 31 gün.
/// </summary>
public sealed class KontakRaporu : IRapor
{
    private const string Sorgu = "EXEC reportPosAsc @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2";
    private const string SurucuSorgu = "SELECT TOP 1 ADSOYAD FROM surucukimlik WITH (NOLOCK) WHERE KIMLIKID = @kimlik";

    public string Kod => "report_ignition";
    public string Baslik => "Sefer Özeti Raporu";
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
        var surucuOnbellek = new Dictionary<string, (bool Bulundu, string? Ad)>();

        double toplamMesafe = 0, toplamSaniye = 0;
        string? surucu = null;
        DateTime? kaTarih = null, kkTarih = null;
        string? kaKonum = null, kkKonum = null;
        double mesafe1 = 0, mesafe2 = 0;

        foreach (var k in kayitlar)
        {
            var raporkodu = (k.GetValueOrDefault("RAPORKODU") as string ?? "").Trim();
            if (raporkodu is not ("G1" or "G2" or "VI")) continue;

            var plaka = RaporYardimci.Metin(k.GetValueOrDefault("PLAKA")) ?? arac.Plaka;
            var tarih = (DateTime)k["TARIH"]!;
            var konum = RaporYardimci.Metin(k.GetValueOrDefault("KONUM"));
            var mesafe = RaporYardimci.Ondalik(k.GetValueOrDefault("MESAFE")) ?? 0;

            if (raporkodu == "VI")
            {
                // Eskisi gibi: kimlik bulunamazsa önceki sürücü adı korunur.
                var kimlik = Convert.ToString(k.GetValueOrDefault("SITUATIONS"), System.Globalization.CultureInfo.InvariantCulture) ?? "";
                if (!surucuOnbellek.TryGetValue(kimlik, out var s))
                {
                    var satir = await db.TekSatirAsync(SurucuSorgu, new Dictionary<string, object?> { ["kimlik"] = kimlik }, ct);
                    s = (satir is not null, satir is null ? null : RaporYardimci.Metin(satir.GetValueOrDefault("ADSOYAD")));
                    surucuOnbellek[kimlik] = s;
                }
                if (s.Bulundu) surucu = s.Ad;
            }

            if (raporkodu == "G2" && kaTarih is null)
            {
                kaTarih = tarih;
                kaKonum = konum;
                mesafe2 = mesafe;
            }

            if (raporkodu == "G1" && kkTarih is null && kaTarih is not null)
            {
                kkTarih = tarih;
                kkKonum = konum;
                mesafe1 = mesafe;
            }

            if (kaTarih is not null && kkTarih is not null)
            {
                var mesafeMetre = mesafe1 - mesafe2;
                toplamMesafe += mesafeMetre;
                var saniye = Math.Truncate((kkTarih.Value - kaTarih.Value).TotalSeconds);
                toplamSaniye += saniye;

                if (mesafeMetre >= 1000)
                {
                    satirlar.Add(new()
                    {
                        ["plaka"] = plaka,
                        ["surucu"] = surucu,
                        ["acma"] = kaTarih.Value,
                        ["acmakonum"] = kaKonum,
                        ["kapama"] = kkTarih.Value,
                        ["kapamakonum"] = kkKonum,
                        ["sure"] = Math.Round(saniye / 60.0, 1),
                        ["km"] = mesafeMetre / 1000.0,
                    });
                }

                kaTarih = null;
                kkTarih = null;
                surucu = null;
            }
        }

        var seferSayisi = satirlar.Count;
        var toplamKm = toplamMesafe >= 1000 ? toplamMesafe / 1000.0 : 0;
        var toplamDakika = Math.Round(toplamSaniye / 60.0, 1);

        satirlar.Add(new()
        {
            ["plaka"] = null,
            ["surucu"] = null,
            ["acma"] = null,
            ["acmakonum"] = null,
            ["kapama"] = null,
            ["kapamakonum"] = "Toplam",
            ["sure"] = toplamDakika,
            ["km"] = toplamKm,
        });

        return new RaporSonuc(
            [
                new("plaka", "Plaka"),
                new("surucu", "Sürücü"),
                new("acma", "Kontak Açma Tarihi", "tarih"),
                new("acmakonum", "Kontak Açma Konumu"),
                new("kapama", "Kontak Kapama Tarihi", "tarih"),
                new("kapamakonum", "Kontak Kapama Konumu"),
                new("sure", "Seyahat Süresi", "sure"),
                new("km", "Toplam Km", "km"),
            ],
            satirlar,
            $"{seferSayisi} sefer (1 km ve üzeri); toplam {toplamKm:0.##} km");
    }
}
