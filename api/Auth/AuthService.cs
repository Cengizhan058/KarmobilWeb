using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Karmobil.Api.Auth;

/// <summary>
/// Oturum jetonu üretimi. Yetkiler jetona GÖMÜLMEZ; her istekte
/// OturumServisi veritabanından okur (yetki değişikliği anında etkili olsun).
/// </summary>
public sealed class AuthService(AuthKey authKey)
{
    public string JetonUret(int kullaniciId, string kullaniciAdi)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, kullaniciId.ToString()),
            new Claim("kullanici_adi", kullaniciAdi),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var kimlikBilgisi = new SigningCredentials(authKey.SigningKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(authKey.GecerlilikSuresi),
            signingCredentials: kimlikBilgisi);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
