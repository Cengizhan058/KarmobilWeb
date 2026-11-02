using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Isı raporu (eski: data/_raporIsi.asp, _forms_raporIsi.asp, js report_temp).
/// reportPos2 kayıtlarından ısı değeri -30..60 aralığında olanlar listelenir; hız &lt; 6 ise 0 gösterilir.
/// En fazla 30 gün (istemci: fark &gt; 30).
/// </summary>
public sealed class IsiRaporu : IRapor
{
    private const string Sorgu = "EXEC reportPos2 @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2";

    public string Kod => "report_temp";
    public string Baslik => "Isı Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 30;

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
        double? enDusuk = null, enYuksek = null;

        foreach (var k in kayitlar)
        {
            var isi = RaporYardimci.Ondalik(k["ISI"]);
            // Eski kod: if isi>=-30 and isi<=60 (NULL ise satır yazılmaz).
            if (isi is not { } t || t < -30 || t > 60) continue;

            var hiz = RaporYardimci.Ondalik(k["HIZ"]) ?? 0;
            // cint(hiz)<6 then hiz=0 (cint: bankacı yuvarlaması)
            if (Math.Round(hiz, MidpointRounding.ToEven) < 6) hiz = 0;

            satirlar.Add(new()
            {
                ["tarih"] = k["TARIH"] as DateTime?,
                ["plaka"] = RaporYardimci.Metin(k["PLAKA"]) ?? arac.Plaka,
                ["konum"] = RaporYardimci.Metin(k["KONUM"]),
                ["kontak"] = KontakAcik(k["KONTAK"]) ? "AÇIK" : "KAPALI",
                ["hiz"] = hiz,
                ["isi"] = t,
                ["koordinat"] = RaporYardimci.Koordinat(k["Y"], k["X"]),
            });

            enDusuk = enDusuk is null ? t : Math.Min(enDusuk.Value, t);
            enYuksek = enYuksek is null ? t : Math.Max(enYuksek.Value, t);
        }

        var ozet = satirlar.Count == 0
            ? "0 kayıt"
            : $"{satirlar.Count} kayıt, en düşük {enDusuk:0.#} °C, en yüksek {enYuksek:0.#} °C";

        return new RaporSonuc(
            [
                new("tarih", "Tarih", "tarih"),
                new("plaka", "Plaka"),
                new("konum", "Konum"),
                new("kontak", "Kontak"),
                new("hiz", "Hız", "sayi"),
                new("isi", "Isı", "sayi"),
                new("koordinat", "Koordinat", "koordinat"),
            ],
            satirlar,
            ozet);
    }

    /// <summary>Eski kod: if kontak=1 then "AÇIK".</summary>
    private static bool KontakAcik(object? v) => v switch
    {
        null => false,
        bool b => b,
        _ => RaporYardimci.Ondalik(v is string s ? s.Trim() : v) == 1,
    };
}
