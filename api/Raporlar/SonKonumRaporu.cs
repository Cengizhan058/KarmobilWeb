using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Son konumlar (eski: data/_raporSonKonum.asp; istemci report_last).
/// Kullanıcının (userclients) aktif araçlarının son durumunu listeler. Eski ekranda araç/tarih seçimi yoktu;
/// üst menüdeki filo (groupId) ve durum süzgeci (filterId) parametre olarak gelir.
/// Durum kuralı: son sinyal 24 saatten eskiyse "Veri Yok", hız &lt; 5 ise kontağa göre Rolanti/Park, değilse Hareketli.
/// </summary>
public sealed class SonKonumRaporu : IRapor
{
    private const string Sorgu = """
        SELECT c.ID, c.PLAKA, c.DVRIPNO, c.KONTROLTARIH,
               dbo.kadranKm(c.CIHAZID) AS KADRAN_KM,
               (CASE WHEN ABS(DATEDIFF(hour, c.KONTROLTARIH, GETDATE())) > 24 THEN 'Veri Yok'
                     ELSE CASE WHEN c.CURRENTSPEED < 5 THEN CASE WHEN c.KONTAK = '1' THEN 'Rolanti' ELSE 'Park' END
                               ELSE 'Hareketli' END END) AS DURUM,
               DATEDIFF(minute, c.LASTSTOP, GETDATE()) AS PARKSURESI,
               c.HATA, c.GPSHATA, c.CURRENTSPEED,
               CONCAT(dbo.checkPOI(@kullaniciId, c.SKX, c.SKY), ' ', c.SKKONUM) AS KONUM,
               dbo.surucuBul(@kullaniciId, c.SOFOR) AS CSURUCU,
               dbo.filoBul(@kullaniciId, c.ID) AS FILOADI
        FROM clients c WITH (NOLOCK), userclients u WITH (NOLOCK)
        WHERE c.STATUS = 1 AND u.USERID = @kullaniciId AND c.ID = u.CLIENTID
          AND (@grupId IS NULL OR c.ID IN (SELECT CLIENTID FROM groupmembers WITH (NOLOCK) WHERE GROUPID = @grupId))
        ORDER BY c.KONTROLTARIH DESC
        """;

    public string Kod => "report_last";
    public string Baslik => "Son Konumlar";
    public AracSecimi Arac => AracSecimi.Yok;
    public TarihSecimi Tarih => TarihSecimi.Yok;
    public int AzamiGun => 0;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("filterId", "Durum", "secim", "all",
        [
            new RaporSecenek("all", "Hepsi"),
            new RaporSecenek("hareketli", "Hareketli"),
            new RaporSecenek("park", "Park"),
            new RaporSecenek("rolanti", "Rölanti"),
            new RaporSecenek("veri_yok", "Veri Yok"),
        ]),
        new RaporParametre("groupId", "Filo", "secim", "all", [new RaporSecenek("all", "Hepsi")], Kaynak: "gruplar"),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var filtre = istek.Deger("filterId") ?? "all";
        var grup = istek.Deger("groupId");
        int? grupId = null;
        if (grup is not null && grup != "all" && int.TryParse(grup, out var g)) grupId = g;

        var kayitlar = await db.SorguAsync(Sorgu, new Dictionary<string, object?>
        {
            ["kullaniciId"] = istek.Kullanici.Id.ToString(),
            ["grupId"] = grupId,
        }, ct);

        var satirlar = new List<Dictionary<string, object?>>();
        foreach (var k in kayitlar)
        {
            var hizHam = k["CURRENTSPEED"] is { } h ? Convert.ToInt64(h) : 0L;
            var konum = RaporYardimci.Metin(k["KONUM"]);
            if ((k["HATA"] as string)?.Trim() == "9") konum = "GPRS Bağlantı Hatası";
            var gpsHata = (k["GPSHATA"]?.ToString() ?? "0").Trim();
            if (gpsHata != "0" && hizHam != 20) konum = "GPS Hatası - Konum verisi alınamıyor";

            var hiz = hizHam < 6 ? 0 : hizHam;
            var durum = (k["DURUM"] as string ?? "").Trim();

            var goster = filtre switch
            {
                "hareketli" => durum == "Hareketli",
                "park" => durum == "Park",
                "rolanti" => durum == "Rolanti",
                "veri_yok" => durum == "Veri Yok",
                _ => true,
            };
            if (!goster) continue;

            if (durum == "Park" && k["PARKSURESI"] is { } p)
                durum = ZamanCevir3(Convert.ToInt64(p)).TrimEnd() + "'dır park halinde";

            var dvr = RaporYardimci.Metin(k["DVRIPNO"]);
            satirlar.Add(new()
            {
                ["tarih"] = k["KONTROLTARIH"] as DateTime?,
                ["plaka"] = RaporYardimci.Metin(k["PLAKA"]),
                ["filo"] = RaporYardimci.Metin(k["FILOADI"]),
                ["surucu"] = RaporYardimci.Metin(k["CSURUCU"]),
                ["kmsayaci"] = RaporYardimci.Ondalik(k["KADRAN_KM"]),
                ["konum"] = konum,
                ["hiz"] = hiz,
                ["durum"] = durum,
                ["dvr"] = dvr == "1.1.1.1" ? null : dvr,
            });
        }

        return new RaporSonuc(
            [
                new("tarih", "Tarih", "tarih"),
                new("plaka", "Plaka"),
                new("filo", "Filo"),
                new("surucu", "Sürücü"),
                new("kmsayaci", "KM Sayacı", "km"),
                new("konum", "Konum"),
                new("hiz", "Hız", "sayi"),
                new("durum", "Durum"),
                new("dvr", "DVR"),
            ],
            satirlar);
    }

    /// <summary>Eski zamanCevir3: dakikayı "X sa Y dk " biçimine çevirir.</summary>
    private static string ZamanCevir3(long dk)
    {
        var saat = dk >= 60 ? dk / 60 : 0;
        var dakika = dk < 60 ? dk : dk - saat * 60;
        var s = dakika >= 0 ? $"{dakika} dk " : "";
        if (saat > 0 && dk >= 60) s = $"{saat} sa " + s;
        return s;
    }
}
