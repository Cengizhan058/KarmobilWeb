using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Rölanti raporu (eski: data/_raporRolanti.asp, _forms_raporRolanti.asp, js report_idling).
/// reportPosAsc kayıtlarında kontak açıkken duruş (VD ya da hız ≤ 5) → hareket (hız &gt; 5 ya da kontak kapalı)
/// geçişlerinden bekleme aralıkları çıkarılır; "Bekleme süresi (dk)" eşiğini geçenler listelenir.
/// Gün sınırı istemcideki "fark &gt; 31" kontrolüdür (sunucudaki rLimit=8 eski kodda sorguyu etkilemiyordu).
/// </summary>
public sealed class RolantiRaporu : IRapor
{
    private const string Sorgu = "EXEC reportPosAsc @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2";

    public string Kod => "report_idling";
    public string Baslik => "Rölanti Raporu";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 31;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
        [new RaporParametre("psure", "Bekleme süresi (dk)", "sayi", "5")];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var arac = istek.Araclar[0];
        var esik = istek.Sayi("psure", 30);

        List<RaporKolon> kolonlar =
        [
            new("plaka", "Plaka"),
            new("baslangic", "Başlangıç", "tarih"),
            new("bitis", "Bitiş", "tarih"),
            new("konum", "Konum"),
            new("sure", "Süre", "sure"),
            new("koordinat", "Koordinat", "koordinat"),
        ];

        // İstemci: psure < 1 ya da sayı değilse "Hatalı bekleme süresi."
        if (esik < 1) return new RaporSonuc(kolonlar, [], "Hatalı bekleme süresi.");

        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["cihazId"] = arac.CihazId,
            ["t1"] = RaporIstegi.SqlTarih(istek.Baslangic!.Value),
            ["t2"] = RaporIstegi.SqlTarih(istek.Bitis!.Value),
        }, ct);

        var satirlar = Bekleme(kayitlar, esik, arac.Plaka, koordinatli: true);
        return new RaporSonuc(kolonlar, satirlar, $"{satirlar.Count} rölanti ({esik} dk ve üzeri)");
    }

    /// <summary>
    /// _raporRolanti.asp / _raporwm.asp ortak döngüsü (iki dosyada birebir aynı durum makinesi).
    /// VD: (RAPORKODU='VD' ya da hız ≤ 5) ve kontak '1'; VZ: hız &gt; 5 ya da kontak '0'.
    /// VD→VZ geçişinde süre = DateDiff("n", duruş, hareket); eşik ve üzeri satır olur.
    /// Konum ve koordinat, eski kodda olduğu gibi hareket anındaki kayıttan alınır.
    /// </summary>
    internal static List<Dictionary<string, object?>> Bekleme(
        List<Dictionary<string, object?>> kayitlar, int esik, string varsayilanPlaka, bool koordinatli)
    {
        var satirlar = new List<Dictionary<string, object?>>();
        var onceki = "";
        DateTime durus = default, hareket = default;

        foreach (var k in kayitlar)
        {
            var hiz = RaporYardimci.Ondalik(k["HIZ"]);
            var kontak = KontakMetni(k["KONTAK"]);
            var kod = (k["RAPORKODU"] as string ?? "").Trim();
            var tarih = SaniyeyeKirp((DateTime)k["TARIH"]!);

            var vd = (kod == "VD" || hiz <= 5) && kontak == "1";
            var vz = hiz > 5 || kontak == "0";

            if (onceki == "")
            {
                durus = tarih;
                hareket = tarih;
                if (vd) onceki = "VD";
                if (vz) onceki = "VZ";
            }

            if (vd) kod = "VD";
            if (vz) kod = "VZ";

            // araç durdu
            if (kod == "VD" && onceki == "VZ" && kontak == "1") durus = tarih;
            // araç hareket etti
            if ((kod == "VZ" || kontak == "0") && onceki == "VD") hareket = tarih;

            var dakika = 0;
            if (onceki == "VD" && (kod == "VZ" || kontak == "0"))
                dakika = DakikaFarki(durus, hareket);

            if (dakika >= esik)
            {
                var satir = new Dictionary<string, object?>
                {
                    ["plaka"] = RaporYardimci.Metin(k["PLAKA"]) ?? varsayilanPlaka,
                    ["baslangic"] = durus,
                    ["bitis"] = hareket,
                    ["konum"] = RaporYardimci.Metin(k["KONUM"]),
                    ["sure"] = dakika,
                };
                if (koordinatli) satir["koordinat"] = RaporYardimci.Koordinat(k["Y"], k["X"]);
                satirlar.Add(satir);
            }

            onceki = kod;
        }

        return satirlar;
    }

    /// <summary>Eski kod cstr(KONTAK) ile "1"/"0" karşılaştırır; bit kolon gelirse 1/0'a çevrilir.</summary>
    internal static string KontakMetni(object? v) => v switch
    {
        null => "",
        bool b => b ? "1" : "0",
        _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "",
    };

    /// <summary>Eski kod tarihi "yyyy-MM-dd HH:mm:ss" metnine çevirip kullanır (salise atılır).</summary>
    private static DateTime SaniyeyeKirp(DateTime d) => new(d.Ticks - d.Ticks % TimeSpan.TicksPerSecond, d.Kind);

    /// <summary>VBScript DateDiff("n", a, b): aradaki dakika sınırı sayısı.</summary>
    private static int DakikaFarki(DateTime a, DateTime b) =>
        (int)(b.Ticks / TimeSpan.TicksPerMinute - a.Ticks / TimeSpan.TicksPerMinute);
}
