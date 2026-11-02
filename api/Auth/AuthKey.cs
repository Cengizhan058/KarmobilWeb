using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Karmobil.Api.Auth;

/// <summary>
/// JWT imzalama/doğrulama parametrelerini tek yerden üretir; hem
/// AuthService (jeton üretimi) hem de doğrulama aynı anahtarı kullanır.
///
/// "Auth:SecretKey" boşsa her başlatmada rastgele üretilir ve oturumlar düşer.
/// </summary>
public sealed class AuthKey
{
    public SymmetricSecurityKey SigningKey { get; }
    public TokenValidationParameters ValidationParameters { get; }
    public TimeSpan GecerlilikSuresi { get; }

    private AuthKey(SymmetricSecurityKey signingKey, TimeSpan gecerlilikSuresi)
    {
        SigningKey = signingKey;
        GecerlilikSuresi = gecerlilikSuresi;
        ValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public static AuthKey Olustur(IConfiguration config)
    {
        var secretKey = config["Auth:SecretKey"];
        var saat = config.GetValue<double?>("Auth:TokenGecerlilikSaati") ?? 12;

        var anahtarBaytlari = string.IsNullOrWhiteSpace(secretKey)
            ? RandomNumberGenerator.GetBytes(64)
            : Encoding.UTF8.GetBytes(secretKey);

        return new AuthKey(new SymmetricSecurityKey(anahtarBaytlari), TimeSpan.FromHours(saat));
    }
}
