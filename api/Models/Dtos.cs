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
    int? SinyalYasiSaat,
    string? Adres,
    string? CihazTipi,
    int CihazTuru,
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

public sealed record GecmisNoktaDto(
    long Id,
    DateTime Zaman,
    double Lat,
    double Lon,
    int Hiz,
    double Yon,
    bool Kontak,
    string RaporKodu,
    string Durum,
    string? Alarm,
    string? Adres,
    string? Surucu,
    double? Isi,
    double? KadranKm,
    // Fiziksel olarak imkânsız sıçrama (yanlış saatli / hatalı konumlu kayıt): tabloda kalır, çizgiye girmez.
    bool Sapma = false);

public sealed record DuraklamaDto(DateTime Baslangic, DateTime Bitis, int Dakika, double Lat, double Lon, string? Adres, bool Kontak);

public sealed record GecmisDto(
    int AracId,
    string Plaka,
    DateTime Baslangic,
    DateTime Bitis,
    double ToplamKm,
    int AzamiHiz,
    List<GecmisNoktaDto> Noktalar,
    List<DuraklamaDto> Duraklar);

/// <summary>Görünüm tercihleri; verilmeyen alan değişmez.</summary>
public sealed record GorunumIstegi(int? Etiket, bool? AraclariGrupla, bool? ArizalilariGizle);

public sealed record GrupDto(int Id, string Ad, int? UstId);

public sealed record BayiDto(string Bid, string? Baslik, string? Logo, string? DestekTel, string? DestekWeb);
