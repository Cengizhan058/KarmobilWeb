using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>
/// Esnek rapor (eski: data/_raporEsnek.asp, _forms_raporEsnek.asp).
/// Tek aracın konumlar kayıtları; kontak / hız / ısı / olay-alarm koduna göre süzülür,
/// kullanıcının seçtiği başlıklar (beyaz listeden) gösterilir. İstemci sınırı 7 gün (fark &gt; 7).
/// Eski status_data / fields_data (chr(1) ayrılmış) yerine formdaki her onay kutusu ayrı parametredir.
/// </summary>
public sealed class EsnekRaporu : IRapor
{
    // Tüm başlık kolonları her zaman okunur; gösterilecekler aşağıdaki beyaz listeyle seçilir.
    // Koşul parçaları yalnızca bu dosyadaki sabitlerden eklenir, değerler parametredir.
    private const string Sorgu = """
        SELECT K.ID, K.X AS lng, K.Y AS lat,
               K.TARIH, C.PLAKA, K.CIHAZID, C.SOFOR, K.KONUM,
               (CASE WHEN K.HIZ < 6 THEN CASE WHEN K.kontak = '1' THEN 'Rolanti' ELSE 'Park' END ELSE 'Hareketli' END) AS DURUM,
               K.KONTAK, K.HIZ, K.Y, K.X, K.ISI, K.RAPORKODU, K.ALARM
        FROM clients C WITH (NOLOCK), konumlar K WITH (NOLOCK)
        WHERE K.CIHAZID = C.CIHAZID AND C.STATUS = 1 AND C.CIHAZID = @cihazId
          AND K.TARIH BETWEEN CONVERT(smalldatetime, @t1, 120) AND CONVERT(smalldatetime, @t2, 120)
        """;

    private const string KontakKosulu = " AND K.kontak = @kontak";
    private const string HizKucuk = " AND K.HIZ < @hiz";
    private const string HizEsit = " AND K.HIZ = @hiz";
    private const string HizBuyuk = " AND K.HIZ > @hiz";
    private const string IsiKucuk = " AND K.ISI < @isi";
    private const string IsiEsit = " AND K.ISI = @isi";
    private const string IsiBuyuk = " AND K.ISI > @isi";
    private const string RaporKoduKosulu = " AND RAPORKODU IN (@@KODLAR@@)";
    private const string Siralama = " ORDER BY TARIH DESC";

    /// <summary>Olay/alarm onay kutuları → RAPORKODU değerleri (eski: 'VZ' → 'VZ','VM','AH').</summary>
    private static readonly (string Ad, string Baslik, string[] Kodlar)[] Durumlar =
    [
        ("status_vd", "Duruyor", ["VD"]),
        ("status_vh", "Harekete Geçti", ["VH"]),
        ("status_vz", "Hareketli", ["VZ", "VM", "AH"]),
        ("status_g1", "Kontak Açma", ["G1"]),
        ("status_g2", "Kontak Kapama", ["G2"]),
        ("alarms_xd", "Duraklama Alarmı", ["XD"]),
        ("alarms_xh", "Hareket Alarmı", ["XH"]),
        ("alarms_ah", "Hız İhlali", ["AH"]),
        ("alarms_aa", "Panik Alarm", ["AA"]),
        ("alarms_ba", "Bölge İhlali", ["BA"]),
    ];

    /// <summary>Başlık beyaz listesi, eski form sırasıyla: parametre adı, sorgu kolonu, kolon tanımı, varsayılan.</summary>
    private static readonly (string Ad, string Alan, RaporKolon Kolon, bool Varsayilan)[] Alanlar =
    [
        ("fields_tarih", "TARIH", new("tarih", "Tarih", "tarih"), true),
        ("fields_plaka", "PLAKA", new("plaka", "Plaka"), true),
        ("fields_cihazid", "CIHAZID", new("cihazid", "IMEI No"), false),
        ("fields_sofor", "SOFOR", new("surucu", "Sürücü"), false),
        ("fields_konum", "KONUM", new("adres", "Adres"), true),
        ("fields_durum", "DURUM", new("durum", "Durum"), false),
        ("fields_kontak", "KONTAK", new("kontak", "Kontak"), true),
        ("fields_hiz", "HIZ", new("hiz", "Hız", "sayi"), true),
        ("fields_y", "Y", new("enlem", "Enlem", "sayi"), false),
        ("fields_x", "X", new("boylam", "Boylam", "sayi"), false),
        ("fields_isi", "ISI", new("isi", "Isı", "sayi"), false),
        ("fields_raporkodu", "RAPORKODU", new("raporkodu", "Rapor Kodu"), false),
        ("fields_alarm", "ALARM", new("alarm", "Alarm"), false),
    ];

    private static readonly RaporSecenek[] Karsilastirma =
        [new("all", "Hepsi"), new("0", "<"), new("1", "="), new("2", ">")];

    public string Kod => "report_custom";
    public string Baslik => "Esnek Rapor";
    public AracSecimi Arac => AracSecimi.Tek;
    public TarihSecimi Tarih => TarihSecimi.Aralik;
    public int AzamiGun => 7;

    public IReadOnlyList<RaporParametre> Parametreler { get; } =
    [
        new RaporParametre("kontak", "Kontak", "secim", "all", [new("all", "Hepsi"), new("0", "Kapalı"), new("1", "Açık")]),
        new RaporParametre("hiz2", "Hız", "secim", "all", Karsilastirma),
        new RaporParametre("hiz", "Hız değeri", "sayi"),
        new RaporParametre("isi2", "Isı", "secim", "all", Karsilastirma),
        new RaporParametre("isi", "Isı değeri", "sayi"),
        .. Durumlar.Select(d => new RaporParametre(d.Ad, d.Baslik, "onay", "1")),
        .. Alanlar.Select(a => new RaporParametre(a.Ad, a.Kolon.Baslik, "onay", a.Varsayilan ? "1" : "0")),
    ];

    public async Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct)
    {
        var secilenAlanlar = Alanlar.Where(a => istek.Onay(a.Ad)).ToList();
        var kolonlar = secilenAlanlar.Select(a => a.Kolon).Append(new RaporKolon("koordinat", "Koordinat", "koordinat")).ToList();

        // Eski istemci denetimleri.
        if (secilenAlanlar.Count == 0)
            return new RaporSonuc(kolonlar, [], "En az 1 adet rapor başlığı seçmelisiniz.");
        var kodlar = Durumlar.Where(d => istek.Onay(d.Ad)).SelectMany(d => d.Kodlar).ToList();
        if (kodlar.Count == 0)
            return new RaporSonuc(kolonlar, [], "En az 1 adet araç durumu veya alarm seçmelisiniz.");

        var prm = new Dictionary<string, object?>
        {
            ["cihazId"] = istek.Araclar[0].CihazId,
            ["t1"] = RaporIstegi.SqlTarih(istek.Baslangic!.Value),
            ["t2"] = RaporIstegi.SqlTarih(istek.Bitis!.Value),
        };
        var sql = new System.Text.StringBuilder(Sorgu);

        // Kontak: eski K.kontak='<cint(kontak)>' (metin karşılaştırma).
        var kontak = istek.Deger("kontak") ?? "all";
        if (kontak != "all")
        {
            if (TamSayi(kontak) is not { } kt) return new RaporSonuc(kolonlar, [], "Geçersiz kontak seçimi.");
            sql.Append(KontakKosulu);
            prm["kontak"] = kt.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var hizKosul = (istek.Deger("hiz2") ?? "all") switch { "0" => HizKucuk, "1" => HizEsit, "2" => HizBuyuk, _ => null };
        if (hizKosul is not null)
        {
            if (TamSayi(istek.Deger("hiz")) is not { } hiz) return new RaporSonuc(kolonlar, [], "Hız değeri giriniz.");
            sql.Append(hizKosul);
            prm["hiz"] = hiz;
        }

        var isiKosul = (istek.Deger("isi2") ?? "all") switch { "0" => IsiKucuk, "1" => IsiEsit, "2" => IsiBuyuk, _ => null };
        if (isiKosul is not null)
        {
            if (TamSayi(istek.Deger("isi")) is not { } isi) return new RaporSonuc(kolonlar, [], "Isı değeri giriniz.");
            sql.Append(isiKosul);
            prm["isi"] = isi;
        }

        var kodAdlari = new List<string>(kodlar.Count);
        for (var i = 0; i < kodlar.Count; i++)
        {
            prm["r" + i] = kodlar[i];
            kodAdlari.Add("@r" + i);
        }
        sql.Append(RaporKoduKosulu.Replace("@@KODLAR@@", string.Join(",", kodAdlari)));
        sql.Append(Siralama);

        var kayitlar = await db.SorguAsync(sql.ToString(), prm, ct);
        var satirlar = new List<Dictionary<string, object?>>(kayitlar.Count);

        foreach (var k in kayitlar)
        {
            var satir = new Dictionary<string, object?>();
            foreach (var a in secilenAlanlar)
            {
                var v = k[a.Alan];
                satir[a.Kolon.Ad] = a.Alan switch
                {
                    "TARIH" => v as DateTime?,
                    "KONTAK" => KontakMetni(v),
                    "HIZ" or "ISI" or "X" or "Y" => RaporYardimci.Ondalik(v),
                    _ => RaporYardimci.Metin(v),
                };
            }
            satir["koordinat"] = RaporYardimci.Koordinat(k["lat"], k["lng"]);
            satirlar.Add(satir);
        }

        return new RaporSonuc(kolonlar, satirlar);
    }

    private static string? KontakMetni(object? v)
    {
        if (v is null) return null;
        return TamSayi(v.ToString()) switch
        {
            0 => "Kapalı",
            1 => "Açık",
            2 => "Yok",
            _ => RaporYardimci.Metin(v),
        };
    }

    /// <summary>VBScript CInt benzeri: sayıya çevirip yarıyı çifte yuvarlar.</summary>
    private static int? TamSayi(string? s) =>
        RaporYardimci.Ondalik(s) is { } d && d is >= -2147483648d and <= 2147483647d
            ? (int)Math.Round(d, MidpointRounding.ToEven)
            : null;
}
