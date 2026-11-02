using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Haritadaki kullanıcı katmanları (salt okuma): bölgeler (data/_bolgeler.asp), nokta grupları/POI
/// (data/_layers.asp) ve rotalar (data/_rotalar.asp). Düzenleme (ekle/sil) ayrı uçlarda yapılacak.
/// </summary>
public static class KatmanEndpoints
{
    private const string Bolgeler = """
        SELECT ID, REGION_NAME, REGION_COLOR, NOKTALAR FROM regions WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY REGION_NAME
        """;

    // _layers.asp: kendi katmanları + ana hesabın (users.GROUPID) alt kullanıcılarla paylaştığı katmanlar.
    private const string Katmanlar = """
        SELECT l.ID, l.LAYERNAME, l.ICONID, l.ICONSIZE, l.ICONCOLOR, l.SUBUSERS, l.USERID
        FROM layers l WITH (NOLOCK)
        WHERE l.USERID = @kullaniciId
           OR (l.SUBUSERS = 1 AND l.USERID = (SELECT GROUPID FROM users WITH (NOLOCK) WHERE ID = @kullaniciId))
        ORDER BY l.LAYERNAME
        """;

    private const string Noktalar = """
        SELECT u.ID, u.LAYERID, u.TITLE, u.DESCRIPTION, u.X, u.Y, u.DIAMETER
        FROM userpoints u WITH (NOLOCK)
        JOIN layers l WITH (NOLOCK) ON l.ID = u.LAYERID
        WHERE l.USERID = @kullaniciId
           OR (l.SUBUSERS = 1 AND l.USERID = (SELECT GROUPID FROM users WITH (NOLOCK) WHERE ID = @kullaniciId))
        """;

    private const string Rotalar = """
        SELECT ID, TITLE, RENK, [ROUTE].ToString() AS POINTS FROM routes WITH (NOLOCK) WHERE USERID = @kullaniciId ORDER BY TITLE
        """;

    public static void MapKatman(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/harita-katmanlari", async (HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            var p = new Dictionary<string, object?> { ["kullaniciId"] = k.Id };

            var bolgeler = (await db.SorguAsync(Bolgeler, p, ct)).Select(s => new
            {
                id = Tam(s["ID"]),
                ad = (s["REGION_NAME"] as string ?? "").Trim(),
                renk = Renk(s["REGION_COLOR"] as string),
                // "boylamxenlem_boylamxenlem_..." → [[enlem, boylam], ...]
                noktalar = (s["NOKTALAR"] as string ?? "").Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Split('x')).Where(n => n.Length == 2 && Sayi(n[0]) is not null && Sayi(n[1]) is not null)
                    .Select(n => new[] { Sayi(n[1])!.Value, Sayi(n[0])!.Value }).ToList(),
            }).Where(b => b.noktalar.Count >= 3).ToList();

            var noktaSatirlari = await db.SorguAsync(Noktalar, p, ct);
            var katmanlar = (await db.SorguAsync(Katmanlar, p, ct)).Select(s =>
            {
                var id = Tam(s["ID"]);
                return new
                {
                    id,
                    ad = (s["LAYERNAME"] as string ?? "").Trim(),
                    renk = Renk(s["ICONCOLOR"] as string),
                    ikon = s["ICONID"]?.ToString(),
                    boyut = s["ICONSIZE"] is null ? 24 : Tam(s["ICONSIZE"]),
                    paylasilan = Tam(s["USERID"]) != k.Id,
                    noktalar = noktaSatirlari.Where(n => Tam(n["LAYERID"]) == id && Sayi(n["X"]) is not null && Sayi(n["Y"]) is not null)
                        .Select(n => new
                        {
                            id = Tam(n["ID"]),
                            baslik = (n["TITLE"] as string ?? "").Trim(),
                            aciklama = (n["DESCRIPTION"] as string)?.Trim(),
                            lat = Sayi(n["Y"])!.Value,
                            lon = Sayi(n["X"])!.Value,
                            cap = n["DIAMETER"] is null ? (int?)null : Tam(n["DIAMETER"]),
                        }).ToList(),
                };
            }).ToList();

            var rotalar = (await db.SorguAsync(Rotalar, p, ct)).Select(s => new
            {
                id = Tam(s["ID"]),
                ad = (s["TITLE"] as string ?? "").Trim(),
                renk = Renk(s["RENK"] as string),
                noktalar = WktCizgi(s["POINTS"] as string),
            }).Where(r => r.noktalar.Count >= 2).ToList();

            return Results.Ok(new { bolgeler, katmanlar, rotalar });
        })
        .WithTags("Katmanlar")
        .RequireAuthorization();
    }

    // "LINESTRING (boylam enlem, boylam enlem)" → [[enlem, boylam], ...]
    private static List<double[]> WktCizgi(string? wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt)) return [];
        var ic = wkt.Trim();
        var a = ic.IndexOf('('); var b = ic.LastIndexOf(')');
        if (a < 0 || b <= a) return [];
        return ic[(a + 1)..b].Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(n => n.Length >= 2 && Sayi(n[0]) is not null && Sayi(n[1]) is not null)
            .Select(n => new[] { Sayi(n[1])!.Value, Sayi(n[0])!.Value }).ToList();
    }

    private static string Renk(string? v)
    {
        var r = (v ?? "").Trim().TrimStart('#');
        return r.Length is 3 or 6 && r.All(Uri.IsHexDigit) ? "#" + r : "#2b8fd6";
    }

    private static int Tam(object? v) => Convert.ToInt32(v, CultureInfo.InvariantCulture);

    private static double? Sayi(object? v) =>
        v is null ? null
        : v is string s ? (double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null)
        : Convert.ToDouble(v, CultureInfo.InvariantCulture);
}
