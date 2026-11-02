using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Models;

namespace Karmobil.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api/auth").WithTags("Auth");

        grup.MapPost("/login", async (GirisIstegi istek, SqlDb db, AuthService authService, CancellationToken ct) =>
        {
            // Kullanıcı adı var/yok ayrımını dışarı sızdırmamak için her
            // başarısızlıkta aynı mesaj döner (eski site metniyle aynı).
            const string genelHata = "Geçersiz kullanıcı adı veya şifre.";

            if (string.IsNullOrWhiteSpace(istek.KullaniciAdi) || string.IsNullOrEmpty(istek.Sifre))
                return Results.BadRequest(new { error = genelHata });

            var satirlar = await db.SorguAsync(
                Sql.KullaniciGiris,
                new Dictionary<string, object?> { ["kullaniciAdi"] = istek.KullaniciAdi.Trim(), ["sifre"] = istek.Sifre },
                ct);

            // login.asp yalnızca tam olarak bir eşleşmede girişe izin verir.
            if (satirlar.Count != 1) return Results.BadRequest(new { error = genelHata });
            var s = satirlar[0];

            // logincheck.asp: YACT=0 → uyari.html (hesap pasif); hiçbir yetkisi yoksa girişe dönülür.
            if (Convert.ToInt64(s["YACT"]) == 0)
                return Results.BadRequest(new { error = "Hesabınız pasif durumda. Lütfen destek ile iletişime geçin.", code = "HESAP_PASIF" });

            var yetkiYok = new[] { "YA", "YW", "YG", "YR", "YP", "YH" }.All(k => Convert.ToInt64(s[k]) == 0);
            if (yetkiYok) return Results.BadRequest(new { error = genelHata });

            var token = authService.JetonUret(Convert.ToInt32(s["ID"]), (string)s["USERNAME"]!);
            return Results.Ok(new { token });
        }).AllowAnonymous();

        grup.MapGet("/ben", async (HttpContext http, OturumServisi oturum, CancellationToken ct) =>
        {
            var kullanici = await oturum.KullaniciAlAsync(http.User, ct);
            return kullanici is null ? Results.Unauthorized() : Results.Ok(kullanici);
        }).RequireAuthorization();

        // Giriş ekranındaki destek telefonu/web adresi (oturumsuz).
        app.MapGet("/api/bayi", async (SqlDb db, CancellationToken ct) =>
        {
            var s = await db.TekSatirAsync(Sql.BayiBilgisi, new Dictionary<string, object?> { ["bid"] = "karmobil" }, ct);
            if (s is null) return Results.NotFound();
            return Results.Ok(new BayiDto(
                (string)s["BID"]!, s["TITLE"] as string, s["LOGO"] as string,
                s["DESTEKTEL"] as string, s["DESTEKWEB"] as string));
        }).WithTags("Auth").AllowAnonymous();
    }
}
