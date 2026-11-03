using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Gider yönetimi (FMS): eski js/fms07.js formları, data/_grid*.asp (liste) ve data/_prc*.asp (kayıt).
/// Eski uçlar yalnız ekleme yapıyordu, sahiplik denetimi yoktu ve SQL'e doğrudan metin ekliyordu.
/// Burada kayıtlar kullanıcının gider takibi açık (clients.FMS = 1) araçlarıyla sınırlı; düzeltme ve silme de var.
/// Kayıtlar eskisi gibi CIHAZID ile tutulur.
/// </summary>
public static class GiderEndpoints
{
    /// <summary>Tip: tarih, tarihSaat, metin, not, tamSayi, sayi, tutar, secim. Uz: nvarchar boyu.</summary>
    public sealed record GiderAlani(string Kolon, string Baslik, string Tip, int Uz = 0, string[]? Secenekler = null, bool Zorunlu = false);

    public sealed record GiderTuru(string Kod, string Baslik, string Tablo, GiderAlani[] Alanlar);

    private static readonly string[] Aylar = Enumerable.Range(1, 24).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();
    private static readonly string[] Adetler = Enumerable.Range(1, 10).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();
    private static string[] Yillar() => Enumerable.Range(2015, DateTime.Today.Year - 2015 + 2).Reverse().Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();

    private static GiderAlani Tarih(string baslik = "Tarih") => new("TARIH", baslik, "tarih", Zorunlu: true);
    private static GiderAlani Km(string tip = "sayi") => new("KILOMETRE", "Kilometre", tip);
    private static GiderAlani Tutar(string baslik = "Tutar (TL)") => new("FIYAT", baslik, "tutar");
    private static readonly GiderAlani Notlar = new("ACIKLAMA", "Notlar", "not", 1000);

    /// <summary>Alan ve seçenekler eski fms07.js formlarıyla aynı; tablo ve kolon adları sabittir (SQL'e yalnız buradan girer).</summary>
    public static IReadOnlyList<GiderTuru> Turler() =>
    [
        new("sigorta", "Sigortalar", "fms_sigorta",
        [
            Tarih("Poliçe tarihi"), new("POLICENO", "Poliçe no", "metin", 100), new("SIGORTAFIRMASI", "Sigorta firması", "metin", 100),
            new("TIPI", "Tipi", "secim", 40, ["KASKO", "TRAFIK"]), new("SURESI", "Süresi (ay)", "secim", 0, Aylar),
            Tutar("Fiyat (TL)"), new("TRAMERNO", "Tramer no", "metin", 100), new("TELEFON", "Telefon", "metin", 100),
            new("ACENTA", "Acenta", "metin", 100), Notlar,
        ]),
        new("yakit", "Yakıt Alımları", "fms_yakit",
        [
            new("TARIH", "Alım tarihi", "tarihSaat", Zorunlu: true), Km(), new("ALAN", "Yakıt alan", "metin", 100),
            new("LITRE", "Miktar (litre)", "sayi"),
            new("TURU", "Türü", "secim", 100, ["Dizel", "Euro Dizel", "Kurşunsuz 95", "Kurşunsuz 98", "LPG", "Normal Benzin", "SHELL FUELSAVE DIESEL", "SHELL V POWER DIESEL", "Süper Benzin"]),
            new("ODEMESEKLI", "Ödeme şekli", "secim", 100, ["Nakit", "Kredi Kartı", "Şirket K.Kartı", "Shell Taşıt Tanıma", "Yakıt Kart", "Diğer"]),
            Tutar("Fiyat (TL)"), Notlar,
        ]),
        new("muayene", "Muayene", "fms_muayene",
        [
            Tarih(), Km(), new("YAPTIRAN", "Yaptıran", "metin", 100), new("YAPILDIGIYER", "Yapıldığı yer", "metin", 128),
            new("ISLEM", "İşlem", "metin", 200), Tutar("Fiyat (TL)"), Notlar,
        ]),
        new("periyodik", "Periyodik Bakım", "fms_periyodik",
        [
            Tarih(), Km(), new("YAPTIRAN", "Yaptıran", "metin", 100), new("YAPILDIGIYER", "Yapıldığı yer", "metin", 128),
            new("ISLEM", "İşlem", "metin", 200), Tutar("Fiyat (TL)"), Notlar,
        ]),
        new("lastik", "Lastik Değişimi", "fms_lastik",
        [
            Tarih(), new("DEGISIMYERI", "Değişim yeri", "metin", 128), new("DEGISIMNEDENI", "Değişim nedeni", "metin", 128),
            new("HANGILASTIK", "Hangi lastik", "secim", 128, ["Ön", "Arka", "Diğer"]), new("ADET", "Adet", "secim", 0, Adetler),
            new("MARKA", "Marka", "metin", 128), new("CAP", "Çap", "metin", 100), Km("tamSayi"), Tutar("Fiyat (TL)"), Notlar,
        ]),
        new("ceza", "Trafik Cezası", "fms_ceza",
        [
            Tarih(), new("TUTANAKNO", "Tutanak no", "metin", 100), new("CEZAMADDESI", "Ceza maddesi", "metin", 100),
            new("CEZAKONUSU", "Ceza konusu", "metin", 200), new("SOFOR", "Şoför", "metin", 100), new("ADRES", "Yer / adres", "metin", 128),
            Tutar(), Notlar,
        ]),
        new("kaza", "Trafik Kazası", "fms_kaza",
        [
            Tarih(), new("TUTANAKNO", "Tutanak no", "metin", 100), new("KAZANEDENI", "Kaza nedeni", "metin", 100),
            new("HASAR", "Hasar", "metin", 200), new("SOFOR", "Şoför", "metin", 100), new("ADRES", "Yer / adres", "metin", 128),
            Tutar(), Notlar,
        ]),
        new("mtv", "MTV Kaydı", "fms_mtv",
        [
            Tarih("Ödendiği tarih"), new("YIL", "Yıl", "secim", 20, Yillar()), new("TAKSIT", "Taksit", "secim", 20, ["1.Taksit", "2.Taksit"]),
            Tutar(), Notlar,
        ]),
        new("ogs", "OGS / HGS Alımı", "fms_ogs",
        [
            Tarih(), new("TURU", "Türü", "secim", 20, ["OGS", "HGS"]), new("ODEMEKANALI", "Ödeme kanalı / banka", "metin", 100),
            Tutar(), Notlar,
        ]),
        new("diger", "Genel Masraf", "fms_digermasraf",
        [
            Tarih(), new("TURU", "Türü", "secim", 20, ["Tamir", "Aksesuar", "Diğer"]), new("MASRAFNEDENI", "Masraf konusu", "metin", 200),
            Tutar(), Notlar,
        ]),
    ];

    public sealed record GiderIstegi(int? AracId, Dictionary<string, string?>? Alanlar);

    // Gider takibi açık, kullanıcıya ait araçlar (eski menü ve formlar da yalnız FMS = 1 araçları gösteriyordu).
    private const string GiderAraclari = """
        SELECT c.ID, c.CIHAZID, c.PLAKA, c.ICON FROM clients c WITH (NOLOCK)
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND c.FMS = 1
        """;

    private const string SahipCihaz = $"CIHAZID IN (SELECT x.CIHAZID FROM ({GiderAraclari}) x)";

    public static void MapGider(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/giderler").WithTags("Gider").RequireAuthorization();

        g.MapGet("/turler", () => Results.Ok(Turler()));

        g.MapGet("/araclar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(GiderAraclari + " ORDER BY c.PLAKA", P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                cihazId = RaporYardimci.Metin(s["CIHAZID"]),
                plaka = RaporYardimci.Metin(s["PLAKA"]),
                ikon = s["ICON"] is null ? 1 : Convert.ToInt32(s["ICON"]),
            }))));

        g.MapGet("/{tur}", (string tur, int arac, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (Bul(tur) is not { } t) return Results.NotFound(new { error = "Gider türü bulunamadı." });
            var p = P(k); p["arac"] = arac;
            var kolonlar = string.Join(", ", t.Alanlar.Select(a => "f." + a.Kolon));
            var sql = $"""
                SELECT f.ID, {kolonlar} FROM {t.Tablo} f WITH (NOLOCK)
                WHERE f.CIHAZID = (SELECT x.CIHAZID FROM ({GiderAraclari}) x WHERE x.ID = @arac)
                ORDER BY f.TARIH DESC, f.ID DESC
                """;
            var satirlar = await db.SorguAsync(sql, p, ct);
            return Results.Ok(satirlar.Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                alanlar = t.Alanlar.ToDictionary(a => a.Kolon, a => Disari(a, s[a.Kolon])),
            }));
        }));

        g.MapPost("/{tur}", (string tur, GiderIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (Bul(tur) is not { } t) return Results.NotFound(new { error = "Gider türü bulunamadı." });
            if (i.AracId is not { } arac) return Hata("Araç seçiniz.");
            var (p, hata) = Degerler(t, i.Alanlar);
            if (hata is not null) return Hata(hata);
            p["kullaniciId"] = k.Id; p["arac"] = arac;
            var kolonlar = string.Join(", ", t.Alanlar.Select(a => a.Kolon));
            var degerler = string.Join(", ", t.Alanlar.Select(a => "@" + a.Kolon));
            var sql = $"""
                INSERT INTO {t.Tablo} (CIHAZID, RECDATE, {kolonlar}{(t.Kod == "sigorta" ? ", UYARI" : "")})
                OUTPUT INSERTED.ID
                SELECT x.CIHAZID, GETDATE(), {degerler}{(t.Kod == "sigorta" ? ", 0" : "")} FROM ({GiderAraclari}) x WHERE x.ID = @arac
                """;
            var id = await db.TekDegerAsync<int>(sql, p, ct);
            return id == 0
                ? Results.NotFound(new { error = "Araç bulunamadı ya da gider takibi kapalı." })
                : Results.Ok(new { id, mesaj = "Kayıt eklendi." });
        }));

        g.MapPut("/{tur}/{id:int}", (string tur, int id, GiderIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (Bul(tur) is not { } t) return Results.NotFound(new { error = "Gider türü bulunamadı." });
            var (p, hata) = Degerler(t, i.Alanlar);
            if (hata is not null) return Hata(hata);
            p["kullaniciId"] = k.Id; p["id"] = id;
            var sql = $"UPDATE {t.Tablo} SET {string.Join(", ", t.Alanlar.Select(a => $"{a.Kolon} = @{a.Kolon}"))} WHERE ID = @id AND {SahipCihaz}";
            return await db.CalistirAsync(sql, p, ct) > 0
                ? Results.Ok(new { mesaj = "Kayıt güncellendi." })
                : Results.NotFound(new { error = "Kayıt bulunamadı ya da size ait değil." });
        }));

        g.MapDelete("/{tur}/{id:int}", (string tur, int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (Bul(tur) is not { } t) return Results.NotFound(new { error = "Gider türü bulunamadı." });
            var p = P(k); p["id"] = id;
            return await db.CalistirAsync($"DELETE FROM {t.Tablo} WHERE ID = @id AND {SahipCihaz}", p, ct) > 0
                ? Results.Ok(new { mesaj = "Kayıt silindi." })
                : Results.NotFound(new { error = "Kayıt bulunamadı ya da size ait değil." });
        }));
    }

    private static GiderTuru? Bul(string kod) => Turler().FirstOrDefault(t => t.Kod == kod);

    /// <summary>İstemciden gelen metin değerleri kolon tipine çevirir; boş değer NULL olur (tutar boşsa 0).</summary>
    private static (Dictionary<string, object?> P, string? Hata) Degerler(GiderTuru t, Dictionary<string, string?>? gelen)
    {
        var p = new Dictionary<string, object?>();
        foreach (var a in t.Alanlar)
        {
            var v = (gelen?.GetValueOrDefault(a.Kolon) ?? "").Trim();
            if (v.Length == 0)
            {
                if (a.Zorunlu) return (p, $"{a.Baslik} giriniz.");
                p[a.Kolon] = a.Tip == "tutar" ? 0d : null;
                continue;
            }
            switch (a.Tip)
            {
                case "tarih" or "tarihSaat":
                    if (!DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih) || tarih.Year < 2000 || tarih.Year > 2100)
                        return (p, $"{a.Baslik}: geçersiz tarih.");
                    p[a.Kolon] = a.Tip == "tarih" ? tarih.Date : tarih;
                    break;
                case "sayi" or "tutar" or "tamSayi":
                    if (!double.TryParse(v.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var sayi) || sayi < 0 || sayi > 1e10)
                        return (p, $"{a.Baslik}: geçersiz sayı.");
                    p[a.Kolon] = a.Tip == "tamSayi" ? (object)(long)Math.Round(sayi) : Math.Round(sayi, 2);
                    break;
                case "secim" when a.Uz == 0:
                    // Sayısal seçim (süre, adet): int kolon.
                    if (!int.TryParse(v, out var n) || a.Secenekler?.Contains(v) != true) return (p, $"{a.Baslik}: geçersiz seçim.");
                    p[a.Kolon] = n;
                    break;
                case "secim":
                    if (a.Secenekler?.Contains(v) != true) return (p, $"{a.Baslik}: geçersiz seçim.");
                    p[a.Kolon] = v;
                    break;
                default:
                    if (v.Length > a.Uz) return (p, $"{a.Baslik}: en fazla {a.Uz} karakter.");
                    p[a.Kolon] = v;
                    break;
            }
        }
        return (p, null);
    }

    private static object? Disari(GiderAlani a, object? v) => v switch
    {
        null => null,
        DateTime t when a.Tip == "tarih" => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime t => t.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
        string s => RaporYardimci.Metin(s),
        _ => v,
    };

    private static Dictionary<string, object?> P(OturumKullanici k) => new() { ["kullaniciId"] = k.Id };
    private static IResult Hata(string m) => Results.BadRequest(new { error = m });

    private static async Task<IResult> Kullanici(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }
}
