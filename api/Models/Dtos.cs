namespace Karmobil.Api.Models;

public sealed record GirisIstegi(string KullaniciAdi, string Sifre);

/// <summary>Durum: Hareketli | Rolanti | Park | VeriYok (data/_jsondata.asp kuralı).</summary>
public sealed record AracKonumDto(
    int Id,
    string CihazId,
    string Plaka,
    string Durum,
    bool Kontak,
    double? Lat,
    double? Lon,
    double Hiz,
    double Yon,
    bool GpsHatasi,
    string? Batarya,
    string? Isi,
    string? Surucu,
    DateTime? GpsTarihi,
    DateTime? SinyalTarihi,
    string? Adres,
    string? CihazTipi,
    List<int> GrupIdleri,
    bool Dvr,
    bool MotorBlokeli,
    double? GunlukKm,
    double? KadranKm);

public sealed record AracDetayDto(
    int Id,
    string CihazId,
    string Plaka,
    string CihazTanimi,
    string? Gsm,
    string? Imei,
    string? AracSahibi,
    string? AracCinsi,
    string? AracMarkasi,
    string? AracTipi,
    string? Aciklama,
    string? DvrAdresi,
    double? Batarya,
    string? MotorBlokaj,
    double? AzamiHiz,
    double? BugunKm,
    bool KontakAcik,
    DateTime? SonKontakKapama,
    string? SonKontakKapamaAdresi,
    int? ParkSuresiDakika,
    DateTime? SonKontakAcma,
    string? SonKontakAcmaAdresi,
    int? KontakAcikSureDakika,
    string? SonAlarm,
    DateTime? SonAlarmTarihi,
    string? SonAlarmAdresi,
    DateTime? IlkSinyal,
    DateTime? KayitTarihi);

public sealed record GrupDto(int Id, string Ad, int? UstId);

public sealed record BayiDto(string Bid, string? Baslik, string? Logo, string? DestekTel, string? DestekWeb);
