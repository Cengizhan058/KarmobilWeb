using Karmobil.Api.Auth;
using Karmobil.Api.Data;

namespace Karmobil.Api.Raporlar;

/// <summary>Raporun araç seçimi: hiç, tek araç, birden çok araç.</summary>
public enum AracSecimi { Yok, Tek, Cok }

/// <summary>Raporun tarih seçimi: hiç, tarih-saat aralığı, ay (yıl+ay).</summary>
public enum TarihSecimi { Yok, Aralik, Ay }

/// <summary>
/// Formda raporun ek alanı. Tur: sayi | metin | secim | onay.
/// Kaynak verilirse seçenekler (Secenekler'den sonra) GET /api/raporlar/secenekler/{Kaynak} ile kullanıcıya göre doldurulur:
/// alarmTurleri, suruculer, canbusGirisleri, bolgeler, katmanlar, noktalar, gruplar.
/// </summary>
public sealed record RaporParametre(
    string Ad,
    string Baslik,
    string Tur,
    string? Varsayilan = null,
    IReadOnlyList<RaporSecenek>? Secenekler = null,
    string? Kaynak = null);

public sealed record RaporSecenek(string Deger, string Ad);

/// <summary>
/// Tablo kolonu. Tur arayüzde biçimlendirmeyi belirler:
/// metin | sayi | tarih (DateTime) | sure (dakika, sayı) | km (sayı) | koordinat ("lat,lon" metin).
/// </summary>
public sealed record RaporKolon(string Ad, string Baslik, string Tur = "metin");

public sealed record RaporSonuc(
    IReadOnlyList<RaporKolon> Kolonlar,
    IReadOnlyList<Dictionary<string, object?>> Satirlar,
    string? Ozet = null);

/// <summary>Kullanıcının seçtiği ve erişimi doğrulanmış araç.</summary>
public sealed record RaporAraci(int Id, string CihazId, string Plaka);

public sealed class RaporIstegi
{
    public required OturumKullanici Kullanici { get; init; }
    public required IReadOnlyList<RaporAraci> Araclar { get; init; }
    /// <summary>Kullanıcı araç seçmeden "tümü" istedi (çoklu raporlarda); bazı yordamlar bunu 'all' ile alır.</summary>
    public bool TumAraclar { get; init; }
    public DateTime? Baslangic { get; init; }
    public DateTime? Bitis { get; init; }
    /// <summary>Ay seçimli raporlarda ayın ilk günü.</summary>
    public DateTime? Ay { get; init; }
    public required IReadOnlyDictionary<string, string?> Ekstra { get; init; }

    public string? Deger(string ad) => Ekstra.TryGetValue(ad, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
    public int Sayi(string ad, int varsayilan) => int.TryParse(Deger(ad), out var n) ? n : varsayilan;
    public bool Onay(string ad) => Deger(ad) is "1" or "true" or "on";

    /// <summary>Eski saklı yordamların beklediği tarih metni.</summary>
    public static string SqlTarih(DateTime d) => d.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// Bir rapor. Uygulamalar Raporlar/ klasöründe durur ve otomatik bulunur (RaporKaydi).
/// Kod, eski menüdeki kimlikle aynıdır (ör. report_park) - arayüz /rapor/{Kod} adresini kullanır.
/// Her rapor kendi SQL metnini kendi dosyasında tutar; davranış eski data/_raporX.asp ile aynı olmalıdır.
/// </summary>
public interface IRapor
{
    string Kod { get; }
    string Baslik { get; }
    AracSecimi Arac { get; }
    TarihSecimi Tarih { get; }
    /// <summary>Tarih aralığı için gün sınırı (eski rLimit); 0 = sınırsız.</summary>
    int AzamiGun { get; }
    IReadOnlyList<RaporParametre> Parametreler { get; }
    Task<RaporSonuc> CalistirAsync(RaporIstegi istek, SqlDb db, CancellationToken ct);
}

public static class RaporKaydi
{
    public static readonly IReadOnlyDictionary<string, IRapor> Tumu =
        typeof(IRapor).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IRapor).IsAssignableFrom(t))
            .Select(t => (IRapor)Activator.CreateInstance(t)!)
            .ToDictionary(r => r.Kod, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Raporlarda ortak yardımcılar.</summary>
public static class RaporYardimci
{
    private static readonly System.Text.RegularExpressions.Regex KontrolKarakteri =
        new("[\u0080-\u009F​-‏﻿]", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Adres/metin alanlarındaki bozuk kontrol karakterlerini ve "&lt;Null&gt;" kalıntılarını temizler.</summary>
    public static string? Metin(object? v)
    {
        var s = (v as string ?? v?.ToString())?.Trim();
        if (string.IsNullOrEmpty(s) || s == "-" || s.Equals("<null>", StringComparison.OrdinalIgnoreCase)) return null;
        return KontrolKarakteri.Replace(s.Normalize(), "").Replace("<Null>", "").Replace("<null>", "").Replace("<NULL>", "");
    }

    public static double? Ondalik(object? v) =>
        v is null ? null
        : v is string s ? (double.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null)
        : Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);

    public static string? Koordinat(object? lat, object? lon) =>
        Ondalik(lat) is { } a && Ondalik(lon) is { } b
            ? $"{a.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)},{b.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}"
            : null;
}
