using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Görevli (RFID kimlik) raporu (eski: data/_raporKimlik.asp, _forms_raporKimlik.asp).
/// raporKimlik yordamı sürücü kimliği + araç bazında günlük rölanti/hareket süresi, azami hız ve mesafe döndürür.
/// Sunucu sınırı (rLimit) 31 gün.
/// </summary>
public sealed class KimlikRaporu : IRapor
{
    // Eski: set dateformat ymd; raporKimlik uid, 'clients', 'kimlik_id', 'tarih1', 'tarih2'
    private const string Sorgu = "SET DATEFORMAT ymd; EXEC raporKimlik @kullaniciId, @cihazId, @kimlikId, @t1, @t2";

    public string Kod => "report_rfid";
    public string Baslik => "Görevli Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    // Eski formdaki seçenekler kullanıcının surucukimlik kayıtlarından (KIMLIKID/ADSOYAD) gelir; "all" = hepsi.
    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("kimlik_id", "Kimlik", "metin", "all")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var kimlik = istek.Deger("kimlik_id") ?? "all";
        var t1 = RaporIstegi.SqlTarih(istek.Baslangic!.Value);
        var t2 = RaporIstegi.SqlTarih(istek.Bitis!.Value);

        // Yordam tek araç (CIHAZID) ya da 'all' alır. Birden çok araçta 'all' ile çağrılıp seçilen plakalarla süzülür.
        var tekArac = istek.Araclar.Count == 1;
        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["kullaniciId"] = istek.Kullanici.Id,
            ["cihazId"] = tekArac ? istek.Araclar[0].CihazId : "all",
            ["kimlikId"] = kimlik,
            ["t1"] = t1,
            ["t2"] = t2,
        }, ct);

        var plakalar = istek.Araclar.Select(a => a.Plaka.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var satirlar = new List<Dictionary<string, object?>>();

        foreach (var k in kayitlar)
        {
            var plaka = RaporYardimci.Metin(k["PLAKA"]);
            if (!tekArac && (plaka is null || !plakalar.Contains(plaka))) continue;

            satirlar.Add(new()
            {
                ["tarih"] = k["TARIH"] is DateTime t ? t.Date : null,
                ["surucu"] = RaporYardimci.Metin(k["SURUCU"]),
                ["plaka"] = plaka,
                ["rolanti"] = RaporYardimci.Ondalik(k["ROLANTI_SURE"]),
                ["hareket"] = RaporYardimci.Ondalik(k["HAREKET_SURE"]),
                ["maxhiz"] = RaporYardimci.Ondalik(k["MAX_HIZ"]),
                ["mesafe"] = RaporYardimci.Ondalik(k["TOPLAM_MESAFE"]),
            });
        }

        return new RaporSonuc(
            [
                new("tarih", "Tarih", "tarih"),
                new("surucu", "Ad Soyad"),
                new("plaka", "Plaka"),
                new("rolanti", "Rölanti Süresi (dk)", "sure"),
                new("hareket", "Hareket Süresi (dk)", "sure"),
                new("maxhiz", "Maksimum Hız", "sayi"),
                new("mesafe", "Toplam Mesafe (km)", "km"),
            ],
            satirlar);
    }
}
