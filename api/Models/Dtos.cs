namespace Karmobil.Api.Models;

public sealed record GirisIstegi(string KullaniciAdi, string Sifre);

public sealed record BayiDto(string Bid, string? Baslik, string? Logo, string? DestekTel, string? DestekWeb);
