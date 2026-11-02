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

public sealed record GrupDto(int Id, string Ad, int? UstId);

public sealed record BayiDto(string Bid, string? Baslik, string? Logo, string? DestekTel, string? DestekWeb);
