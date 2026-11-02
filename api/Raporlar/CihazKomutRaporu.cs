using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Cihaz programlama geçmişi (eski: data/_raporCihazKomut.asp, _forms_raporCihazKomut.asp, cihazkomut_logs()).
/// reportCihazKomutLogs yordamı; STATUS 0 = komut verildi, 1 = cihaz işleme aldı.
/// Eski formda tek araç ya da "Hepsi" seçilebiliyordu; burada tümü seçiliyse 'all', değilse araç başına çağrılır.
/// </summary>
public sealed class CihazKomutRaporu : IRapor
{
    private const string Sorgu =
        "EXEC reportCihazKomutLogs @userid = @kullaniciId, @cihazid = @cihazId, @raporkodu = @raporKodu, @tarih1 = @t1, @tarih2 = @t2";

    public string Kod => "cihazkomut_logs";
    public string Baslik => "Cihaz Programlama Geçmişi";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("alarms", "Programlama türü", "secim", "all", [new RaporSecenek("all", "Hepsi")], Kaynak: "komutKodlari")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var hedefler = istek.TumAraclar ? ["all"] : istek.Araclar.Select(a => a.CihazId).ToArray();
        var kayitlar = new List<Dictionary<string, object?>>();
        foreach (var cihazId in hedefler)
        {
            kayitlar.AddRange(await db.SorguAsync(Sorgu, new Dictionary<string, object?>
            {
                ["kullaniciId"] = istek.Kullanici.Id,
                ["cihazId"] = cihazId,
                ["raporKodu"] = istek.Deger("alarms") ?? "all",
                ["t1"] = RaporIstegi.SqlTarih(istek.Baslangic!.Value),
                ["t2"] = RaporIstegi.SqlTarih(istek.Bitis!.Value),
            }, ct));
        }

        var satirlar = kayitlar
            .OrderByDescending(k => k.GetValueOrDefault("TARIH") as DateTime? ?? DateTime.MinValue)
            .Select(k => new Dictionary<string, object?>
            {
                ["tarih"] = k.GetValueOrDefault("TARIH") as DateTime?,
                ["plaka"] = RaporYardimci.Metin(k.GetValueOrDefault("PLAKA")),
                ["aciklama"] = RaporYardimci.Metin(k.GetValueOrDefault("ACIKLAMA")),
                ["durum"] = Convert.ToString(k.GetValueOrDefault("STATUS"))?.Trim() switch
                {
                    "0" => "Komut verildi",
                    "1" => "İşleme alındı",
                    var d => d,
                },
            }).ToList();

        return new RaporSonuc(Kolonlar, satirlar);
    }

    private static readonly IReadOnlyList<RaporKolon> Kolonlar =
    [
        new("tarih", "Tarih", "tarih"),
        new("plaka", "Plaka"),
        new("aciklama", "Açıklama"),
        new("durum", "Durum"),
    ];
}
