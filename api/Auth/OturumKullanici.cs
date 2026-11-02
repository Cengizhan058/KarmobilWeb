namespace Karmobil.Api.Auth;

/// <summary>
/// Yetkiler users tablosundaki bayrak kolonlarından gelir (logincheck.asp ile aynı anlam):
/// YA araç yönetimi, YG grup yönetimi, YR raporlar, YP cihaz programlama,
/// YH şifre/kullanıcı ayarları, YW harita, YC komut.
/// </summary>
public sealed record OturumKullanici(
    int Id,
    string KullaniciAdi,
    string? AdSoyad,
    string Seviye,
    string? FirmaKodu,
    bool AracYonetimi,
    bool GrupYonetimi,
    bool Raporlar,
    bool CihazProgramlama,
    bool KullaniciAyarlari,
    bool Harita,
    bool Komut,
    bool Supervisor,
    int AltKullaniciLimiti,
    bool AkilliBlokaj,
    HaritaSiniri? HaritaSiniri,
    string? Tema);

public sealed record HaritaSiniri(double X1, double Y1, double X2, double Y2);
