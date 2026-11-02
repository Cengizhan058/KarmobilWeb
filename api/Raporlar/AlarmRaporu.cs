using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Alarm/İhlal raporu (eski: data/_raporAlarm.asp, _forms_raporAlarm.asp, report_alarm()).
/// reportAlarm yordamının kayıtları listelenir; hız 6'nın altındaysa 0 yazılır, ısı 999 ise boş bırakılır,
/// G1/G2 rapor kodları "Kontak Kapandı"/"Kontak Açıldı" olarak adlandırılır. En fazla 31 gün.
/// Eski formda araç ya tek araç ya da "Hepsi" seçilebiliyordu; tek araçta yordama cihaz kimliği,
/// birden çok araçta 'all' geçilir ve sonuç seçilen araçlarla süzülür (eski "Hepsi" davranışı).
/// </summary>
public sealed class AlarmRaporu : IRapor
{
    private const string Sorgu =
        "EXEC reportAlarm @userid = @kullaniciId, @cihazid = @cihazId, @raporkodu = @raporKodu, @tarih1 = @t1, @tarih2 = @t2";

    public string Kod => "report_alarms";
    public string Baslik => "Alarm/İhlal Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    // Eski formda seçenekler raportanim tablosundan (RAPORKODU/RAPORADI) doluyordu; "all" = Hepsi.
    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("alarms", "Alarm/İhlal türü (rapor kodu, all = hepsi)", "metin", "all")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var raporKodu = istek.Deger("alarms") ?? "all";
        var cokArac = istek.Araclar.Count > 1;

        if (cokArac && raporKodu.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Eski istemcideki kısıt: tüm araçlar + tüm alarm türleri birlikte sorgulanamaz.
            return new RaporSonuc(Kolonlar, [],
                "Birden çok araç için yalnızca bir alarm türüne göre sorgulama yapabilirsiniz.");
        }

        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["kullaniciId"] = istek.Kullanici.Id,
            ["cihazId"] = cokArac ? "all" : istek.Araclar[0].CihazId,
            ["raporKodu"] = raporKodu,
            ["t1"] = RaporIstegi.SqlTarih(istek.Baslangic!.Value),
            ["t2"] = RaporIstegi.SqlTarih(istek.Bitis!.Value),
        }, ct);

        var secilen = istek.Araclar.Select(a => a.CihazId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var plakalar = istek.Araclar.GroupBy(a => a.CihazId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Plaka, StringComparer.OrdinalIgnoreCase);

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var k in kayitlar)
        {
            var cihazId = (Convert.ToString(k.GetValueOrDefault("CIHAZID"), System.Globalization.CultureInfo.InvariantCulture) ?? "").Trim();
            if (cokArac && !secilen.Contains(cihazId)) continue;

            var raporkodu = (k.GetValueOrDefault("RAPORKODU") as string ?? "").Trim();
            var alarm = raporkodu switch
            {
                "G1" => "Kontak Kapandı",
                "G2" => "Kontak Açıldı",
                _ => RaporYardimci.Metin(k.GetValueOrDefault("ALARM")),
            };

            var hiz = RaporYardimci.Ondalik(k.GetValueOrDefault("HIZ")) ?? 0;
            if (Math.Round(hiz, MidpointRounding.ToEven) < 6) hiz = 0;

            var isi = RaporYardimci.Ondalik(k.GetValueOrDefault("ISI"));
            if (isi == 999) isi = null;

            var kontak = k.GetValueOrDefault("KONTAK") switch
            {
                bool b => b,
                var v => RaporYardimci.Ondalik(v) == 1,
            };

            satirlar.Add(new()
            {
                ["tarih"] = k.GetValueOrDefault("TARIH") as DateTime?,
                ["plaka"] = RaporYardimci.Metin(k.GetValueOrDefault("PLAKA"))
                            ?? (plakalar.TryGetValue(cihazId, out var p) ? p : null),
                ["konum"] = RaporYardimci.Metin(k.GetValueOrDefault("KONUM")),
                ["kontak"] = kontak ? "AÇIK" : "KAPALI",
                ["hiz"] = hiz,
                ["isi"] = isi,
                ["alarm"] = alarm,
                ["raporkodu"] = raporkodu,
                ["koordinat"] = RaporYardimci.Koordinat(k.GetValueOrDefault("Y"), k.GetValueOrDefault("X")),
            });
        }

        return new RaporSonuc(Kolonlar, satirlar);
    }

    private static readonly IReadOnlyList<RaporKolon> Kolonlar =
    [
        new("tarih", "Tarih", "tarih"),
        new("plaka", "Plaka"),
        new("konum", "Konum"),
        new("kontak", "Kontak"),
        new("hiz", "Hız", "sayi"),
        new("isi", "Isı", "sayi"),
        new("alarm", "Alarm"),
        new("raporkodu", "Rapor Kodu"),
        new("koordinat", "Koordinat", "koordinat"),
    ];
}
