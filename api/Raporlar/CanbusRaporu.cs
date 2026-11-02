using System.Globalization;
using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Can-Bus raporu (eski: data/_raporCanbus.asp, _forms_raporCanbus.asp).
/// canbus kayıtları (yeniden eskiye); değer bir önceki satırdan farklıysa ve "255" değilse listelenir.
/// Sunucu sınırı 31 gün, istemci tek araçta 8 gün (fark &gt; 8) - dar olan uygulanır.
/// </summary>
public sealed class CanbusRaporu : IRapor
{
    private const string Sorgu = """
        SELECT cl.PLAKA, c.INPUT, c.VALUE, c.TARIH, ci.TITLE
        FROM canbus c, canbusinputs ci, clients cl
        WHERE (c.TARIH BETWEEN CONVERT(smalldatetime, @t1, 120) AND CONVERT(smalldatetime, @t2, 120))
          AND cl.STATUS = 1 AND c.INPUT = ci.KOMUT
          AND c.CIHAZID = @cihazId AND cl.CIHAZID = c.CIHAZID
        ORDER BY c.TARIH DESC
        """;

    private const string SorguGirisTipli = """
        SELECT cl.PLAKA, c.INPUT, c.VALUE, c.TARIH, ci.TITLE
        FROM canbus c, canbusinputs ci, clients cl
        WHERE (c.TARIH BETWEEN CONVERT(smalldatetime, @t1, 120) AND CONVERT(smalldatetime, @t2, 120))
          AND cl.STATUS = 1 AND c.INPUT = ci.KOMUT
          AND c.CIHAZID = @cihazId AND cl.CIHAZID = c.CIHAZID
          AND c.INPUT = @gtip
        ORDER BY c.TARIH DESC
        """;

    public string Kod => "report_canbus";
    public string Baslik => "Can-Bus Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 8;

    // Eski formdaki seçenekler canbusinputs tablosundan (KOMUT/TITLE) gelir; boş ya da "all" = tüm girişler.
    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("gtip", "Giriş tipi", "secim", "all", [new RaporSecenek("all", "Hepsi")], Kaynak: "canbusGirisleri")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var arac = istek.Araclar[0];
        var gtip = istek.Deger("gtip");
        if (gtip is not null && (gtip.Equals("all", StringComparison.OrdinalIgnoreCase) || gtip == "sec")) gtip = null;

        var prm = new Dictionary<string, object?>
        {
            ["cihazId"] = arac.CihazId,
            ["t1"] = RaporIstegi.SqlTarih(istek.Baslangic!.Value),
            ["t2"] = RaporIstegi.SqlTarih(istek.Bitis!.Value),
        };
        if (gtip is not null) prm["gtip"] = gtip;

        var kayitlar = await db.SorguAsync(gtip is null ? Sorgu : SorguGirisTipli, prm, ct);

        var satirlar = new List<Dictionary<string, object?>>();
        // Eski kod: oldValue = "gokhanper" ile başlar; NULL değerle karşılaştırma (VBScript Null) satır üretmez.
        string? onceki = "\u0001";
        foreach (var k in kayitlar)
        {
            var deger = DegerMetni(k["VALUE"]);
            if (deger is not null && onceki is not null && deger != onceki && deger != "255")
            {
                satirlar.Add(new()
                {
                    ["tarih"] = k["TARIH"] as DateTime?,
                    ["plaka"] = RaporYardimci.Metin(k["PLAKA"]),
                    ["giris"] = RaporYardimci.Metin(k["TITLE"]),
                    ["deger"] = deger,
                });
            }
            onceki = deger;
        }

        return new RaporSonuc(
            [
                new("tarih", "Tarih", "tarih"),
                new("plaka", "Plaka"),
                new("giris", "Giriş Tipi"),
                new("deger", "Değer"),
            ],
            satirlar);
    }

    private static string? DegerMetni(object? v) => v switch
    {
        null => null,
        string s => s.Trim(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString(),
    };
}
