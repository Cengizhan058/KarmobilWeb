using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Nokta/POI raporu (eski: data/_raporNokta.asp, form report_poi() içinde).
/// Kullanıcının nokta gruplarındaki (layers → userpoints) noktalara araçların varış/ayrılış zamanları.
/// Konum noktaya X ve Y'de 0.0005'ten yakınsa varış; varıştan sonra X VE Y'de 0.0005'ten uzaksa ayrılış.
/// Sunucu sınırı (rLimit) 3 gün.
/// </summary>
public sealed class NoktaRaporu : IRapor
{
    private const double Esik = 0.0005;
    private const int ParcaBoyutu = 1000; // SQL Server 2100 parametre sınırının altında kalmak için

    // Eski: layers (USERID = kullanıcı, isteğe bağlı ID) → her grup için userpoints (LAYERID, isteğe bağlı ID).
    private const string NoktaSorgu = """
        SELECT p.X, p.Y, p.TITLE
        FROM layers l WITH (NOLOCK)
        INNER JOIN userpoints p WITH (NOLOCK) ON p.LAYERID = l.ID
        WHERE l.USERID = @kullaniciId
          AND (@katman IS NULL OR l.ID = @katman)
          AND (@nokta IS NULL OR p.ID = @nokta)
        ORDER BY l.ID, p.ID
        """;

    // @@ARACLAR@@ yalnızca @a0,@a1,... parametre adlarıyla değiştirilir; değerler parametredir.
    private const string KonumSorgu = """
        SELECT clients.ID AS CLID, clients.PLAKA, konumlar.CIHAZID, konumlar.TARIH, konumlar.X, konumlar.Y
        FROM konumlar WITH (NOLOCK), clients WITH (NOLOCK)
        WHERE clients.STATUS = 1
          AND konumlar.TARIH BETWEEN CONVERT(datetime, @t1, 120) AND CONVERT(datetime, @t2, 120)
          AND konumlar.CIHAZID IN (@@ARACLAR@@)
          AND konumlar.CIHAZID = clients.CIHAZID
        """;

    // "Hepsi" seçiminde eski araç listesi GSM / MOB% cihazları dışarıda bırakıyordu.
    private const string KonumHepsiEki = " AND clients.CIHAZTIPI != 'GSM' AND clients.CIHAZID NOT LIKE 'MOB%'";
    private const string KonumSiralama = " ORDER BY konumlar.TARIH ASC";

    public string Kod => "report_poi";
    public string Baslik => "Nokta/POI Raporu";
    public AracSecimi Arac => AracSecimi.Cok;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 3;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("layer", "Nokta grubu", "metin", "all"),
        new RaporParametre("poi", "Nokta", "metin", "all"),
    ];

    private sealed record Nokta(double X, double Y, string? Ad);

    private sealed class AracDurumu
    {
        public int Nokta = -1;
        public DateTime? Varis;
        public DateTime? Ayrilis;
    }

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var kolonlar = new RaporKolon[]
        {
            new("plaka", "Plaka"),
            new("varis", "Varış Zamanı", "tarih"),
            new("ayrilis", "Ayrılış Zamanı", "tarih"),
            new("nokta", "Nokta"),
            new("sure", "Bekleme Süresi", "sure"),
            new("koordinat", "Koordinat", "koordinat"),
        };

        if (!SecimAl(istek.Deger("layer"), out var katman) || !SecimAl(istek.Deger("poi"), out var noktaId))
            return new RaporSonuc(kolonlar, [], "Geçersiz nokta grubu / nokta seçimi.");

        var noktalar = new List<Nokta>();
        foreach (var n in await db.SorguAsync(NoktaSorgu, new Dictionary<string, object?>
                 {
                     ["kullaniciId"] = istek.Kullanici.Id.ToString(), ["katman"] = katman, ["nokta"] = noktaId,
                 }, ct))
        {
            // Koordinatı boş nokta eski kodda hiçbir karşılaştırmayı geçemezdi.
            if (RaporYardimci.Ondalik(n["X"]) is { } x && RaporYardimci.Ondalik(n["Y"]) is { } y)
                noktalar.Add(new Nokta(x, y, RaporYardimci.Metin(n["TITLE"])));
        }

        if (noktalar.Count == 0) return new RaporSonuc(kolonlar, []);

        var t1 = RaporIstegi.SqlTarih(istek.Baslangic!.Value);
        var t2 = RaporIstegi.SqlTarih(istek.Bitis!.Value);
        var hepsiEki = istek.Araclar.Count > 1 ? KonumHepsiEki : "";

        var durumlar = new Dictionary<int, AracDurumu>();
        var bulunan = new List<(DateTime Zaman, int Sira, Dictionary<string, object?> Satir)>();
        var sira = 0;

        foreach (var parca in istek.Araclar.Chunk(ParcaBoyutu))
        {
            var prm = new Dictionary<string, object?> { ["t1"] = t1, ["t2"] = t2 };
            var adlar = new List<string>(parca.Length);
            for (var i = 0; i < parca.Length; i++)
            {
                prm["a" + i] = parca[i].CihazId;
                adlar.Add("@a" + i);
            }
            var sql = KonumSorgu.Replace("@@ARACLAR@@", string.Join(",", adlar)) + hepsiEki + KonumSiralama;

            foreach (var k in await db.SorguAsync(sql, prm, ct))
            {
                var clid = Convert.ToInt32(k["CLID"]);
                var tarih = (DateTime)k["TARIH"]!;
                if (RaporYardimci.Ondalik(k["X"]) is not { } cx || RaporYardimci.Ondalik(k["Y"]) is not { } cy) continue;

                if (!durumlar.TryGetValue(clid, out var d)) durumlar[clid] = d = new AracDurumu();

                // Varış: henüz varış yoksa tüm noktalara bak (eşleşen son nokta geçerli).
                if (d.Varis is null)
                {
                    for (var pn = 0; pn < noktalar.Count; pn++)
                    {
                        if (Math.Abs(cx - noktalar[pn].X) < Esik && Math.Abs(cy - noktalar[pn].Y) < Esik)
                        {
                            d.Nokta = pn;
                            d.Varis = tarih;
                        }
                    }
                }

                // Ayrılış: varış var, ayrılış yok ve hem X hem Y'de noktadan uzaklaşmış.
                if (d.Varis is not null && d.Ayrilis is null && d.Nokta >= 0 &&
                    Math.Abs(cx - noktalar[d.Nokta].X) > Esik && Math.Abs(cy - noktalar[d.Nokta].Y) > Esik)
                    d.Ayrilis = tarih;

                if (d.Nokta >= 0 && d.Varis is { } varis && d.Ayrilis is { } ayrilis)
                {
                    var p = noktalar[d.Nokta];
                    // VBScript DateDiff("n"): geçilen dakika sınırı sayısı.
                    var dakika = (int)(DakikayaKes(ayrilis) - DakikayaKes(varis)).TotalMinutes;
                    bulunan.Add((tarih, sira++, new Dictionary<string, object?>
                    {
                        ["plaka"] = RaporYardimci.Metin(k["PLAKA"]),
                        ["varis"] = varis,
                        ["ayrilis"] = ayrilis,
                        ["nokta"] = p.Ad,
                        ["sure"] = dakika,
                        ["koordinat"] = RaporYardimci.Koordinat(p.Y, p.X),
                    }));
                    d.Nokta = -1;
                    d.Varis = null;
                    d.Ayrilis = null;
                }
            }
        }

        // Eski rapor satırları tek sorguda TARIH sırasıyla yazıyordu; parçalar birleştirilirken aynı sıra korunur.
        var satirlar = bulunan.OrderBy(b => b.Zaman).ThenBy(b => b.Sira).Select(b => b.Satir).ToList();
        return new RaporSonuc(kolonlar, satirlar);
    }

    private static DateTime DakikayaKes(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, d.Kind);

    /// <summary>"all"/boş → filtre yok; aksi halde sayısal kimlik olmalı.</summary>
    private static bool SecimAl(string? deger, out int? id)
    {
        id = null;
        if (deger is null || deger.Equals("all", StringComparison.OrdinalIgnoreCase)) return true;
        if (!int.TryParse(deger, out var n)) return false;
        id = n;
        return true;
    }
}
