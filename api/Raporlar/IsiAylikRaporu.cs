using System.Globalization;
using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Aylık ısı aşım raporu (eski: data/_raporIsiAylik.asp, _forms_raporIsiAylik.asp, js report_temp_monthly).
/// dbo.reportTemp (araç ID, tarih1, tarih2, ısı eşiği, çalışma saati başlangıç/bitiş) her satırı için
/// PLAKA, CIHAZID, TUR ve gün numarası adlı ("1".."31") kolonlar döndürür. Gün kolonları, eski kodda olduğu gibi
/// başlangıç gününden bitiş gününe kadar (yalnızca ayın günü kullanılır; başlangıç tarihi bu ay ise bugünü geçmez).
/// Tarihler yalnızca gün olarak (yyyy-MM-dd) gider. En fazla 31 gün (istemci: fark &gt; 31).
/// </summary>
public sealed class IsiAylikRaporu : IRapor
{
    private const string Sorgu = "EXEC dbo.reportTemp @aracId, @t1, @t2, @esik, @saat1, @saat2";

    public string Kod => "report_temp_monthly";
    public string Baslik => "Aylık Isı Aşım Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    private static readonly IReadOnlyList<RaporSecenek> Saatler =
        [.. Enumerable.Range(0, 24).Select(i => new RaporSecenek(i.ToString(CultureInfo.InvariantCulture), $"{i:00}:00"))];

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("saat1", "Çalışma saati başlangıç", "secim", "7", Saatler),
        new RaporParametre("saat2", "Çalışma saati bitiş", "secim", "15", Saatler),
        new RaporParametre("filter_val", "Isı >=", "sayi", "8"),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var arac = istek.Araclar[0];
        var bas = istek.Baslangic!.Value.Date;
        var bit = istek.Bitis!.Value.Date;
        var saat1 = Math.Clamp(istek.Sayi("saat1", 7), 0, 23);
        var saat2 = Math.Clamp(istek.Sayi("saat2", 15), 0, 23);
        var esik = RaporYardimci.Ondalik(istek.Deger("filter_val")) ?? 0;

        // Gün kolonları: n1..n2 (ayın günü). Başlangıç tarihi içinde bulunulan ay ise n2 bugünü geçmez.
        var n1 = bas.Day;
        var n2 = bit.Day;
        var bugun = DateTime.Now;
        if (bugun.Year == bas.Year && bugun.Month == bas.Month && bugun.Day < n2) n2 = bugun.Day;

        var kolonlar = new List<RaporKolon>
        {
            new("plaka", "Plaka"),
            new("cihazid", "Cihaz ID"),
            new("aciklama", "Açıklama"),
        };
        for (var n = n1; n <= n2; n++) kolonlar.Add(new($"g{n}", n.ToString(CultureInfo.InvariantCulture), "sayi"));

        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["aracId"] = arac.Id,
            ["t1"] = bas.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["t2"] = bit.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["esik"] = esik,
            ["saat1"] = saat1,
            ["saat2"] = saat2,
        }, ct);

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var k in kayitlar)
        {
            var satir = new Dictionary<string, object?>
            {
                ["plaka"] = RaporYardimci.Metin(k["PLAKA"]) ?? arac.Plaka,
                ["cihazid"] = RaporYardimci.Metin(k["CIHAZID"]),
                ["aciklama"] = RaporYardimci.Metin(k["TUR"]),
            };
            for (var n = n1; n <= n2; n++)
            {
                // FormatNumber(val, 1): bir ondalık. "% Uygunluk" satırında eski kod sona "%" ekliyordu;
                // burada değer sayı olarak kalır, birim Açıklama kolonundadır.
                var deger = k.TryGetValue(n.ToString(CultureInfo.InvariantCulture), out var v) ? RaporYardimci.Ondalik(v) : null;
                satir[$"g{n}"] = deger is { } d ? Math.Round(d, 1, MidpointRounding.AwayFromZero) : null;
            }
            satirlar.Add(satir);
        }

        return new RaporSonuc(
            kolonlar,
            satirlar,
            $"{arac.Plaka}: {bas:dd.MM.yyyy} - {bit:dd.MM.yyyy}, çalışma saatleri {saat1:00}:00-{saat2:00}:00, ısı >= {esik.ToString(CultureInfo.InvariantCulture)}");
    }
}
