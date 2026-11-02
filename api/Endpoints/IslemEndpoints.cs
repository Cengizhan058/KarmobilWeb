using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// İşlemler menüsü: nokta grupları (layers), noktalar/POI (userpoints), bölgeler (regions) ve rotalar (routes)
/// için ekle/güncelle/sil. Eski karşılıkları data/_prcLayer.asp, _prcLayerPoints.asp, _prcBolge.asp, _prcRoute.asp.
/// Her yazma yalnızca kullanıcının kendi kaydına yapılır (USERID = oturum kullanıcısı). Eski sitedeki iki açık
/// burada kapatıldı: katman silinirken başka kullanıcının noktaları silinebiliyordu, nokta eklenirken
/// katmanın sahibi denetlenmiyordu.
/// </summary>
public static class IslemEndpoints
{
    // Nokta grupları
    private const string KatmanEkle = """
        INSERT INTO layers (USERID, LAYERNAME, ICONID, ICONSIZE, SUBUSERS) OUTPUT INSERTED.ID
        VALUES (@kullaniciId, @ad, @ikon, @boyut, @paylas)
        """;
    private const string KatmanGuncelle = """
        UPDATE layers SET LAYERNAME = @ad, ICONID = @ikon, ICONSIZE = @boyut, SUBUSERS = @paylas
        WHERE ID = @id AND USERID = @kullaniciId
        """;
    private const string KatmanSil = """
        DELETE FROM userpoints WHERE LAYERID = @id AND LAYERID IN (SELECT ID FROM layers WHERE USERID = @kullaniciId);
        DELETE FROM layers WHERE ID = @id AND USERID = @kullaniciId;
        """;

    // Noktalar (X = boylam, Y = enlem)
    private const string NoktaEkle = """
        INSERT INTO userpoints (LAYERID, TITLE, DIAMETER, X, Y) OUTPUT INSERTED.ID
        SELECT @katmanId, @baslik, @cap, @x, @y WHERE EXISTS (SELECT 1 FROM layers WHERE ID = @katmanId AND USERID = @kullaniciId)
        """;
    private const string NoktaGuncelle = """
        UPDATE userpoints SET TITLE = @baslik, DIAMETER = @cap, X = @x, Y = @y
        WHERE ID = @id AND LAYERID IN (SELECT ID FROM layers WHERE USERID = @kullaniciId)
        """;
    private const string NoktaSil = """
        DELETE FROM userpoints WHERE ID = @id AND LAYERID IN (SELECT ID FROM layers WHERE USERID = @kullaniciId)
        """;

    // Bölgeler (NOKTALAR: "boylamxenlem_boylamxenlem_", renk '#' olmadan)
    private const string BolgeEkle = """
        INSERT INTO regions (REGION_NAME, REGION_COLOR, NOKTALAR, USERID) OUTPUT INSERTED.ID
        VALUES (@ad, @renk, @noktalar, @kullaniciId)
        """;
    private const string BolgeGuncelle = """
        UPDATE regions SET REGION_NAME = @ad, REGION_COLOR = @renk, NOKTALAR = COALESCE(@noktalar, NOKTALAR)
        WHERE ID = @id AND USERID = @kullaniciId
        """;
    private const string BolgeSil = "DELETE FROM regions WHERE ID = @id AND USERID = @kullaniciId";

    // Rotalar (geography LINESTRING, SRID 4326). Eski sitede yalnızca ad/renk düzenlenebiliyordu.
    private const string RotaEkle = """
        INSERT INTO routes (USERID, TITLE, RENK, [ROUTE]) OUTPUT INSERTED.ID
        VALUES (@kullaniciId, @ad, @renk, geography::STLineFromText(@wkt, 4326))
        """;
    private const string RotaGuncelle = "UPDATE routes SET TITLE = @ad, RENK = @renk WHERE ID = @id AND USERID = @kullaniciId";
    private const string RotaSil = "DELETE FROM routes WHERE ID = @id AND USERID = @kullaniciId";

    public sealed record KatmanIstegi(string? Ad, string? Ikon, int Boyut, bool Paylas);
    public sealed record NoktaIstegi(string? Baslik, int? Cap, double Lat, double Lon);
    /// <summary>Noktalar [[enlem, boylam], ...]; güncellemede boş bırakılırsa şekil değişmez.</summary>
    public sealed record SekilIstegi(string? Ad, string? Renk, double[][]? Noktalar);

    public static void MapIslem(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").WithTags("İşlemler").RequireAuthorization();

        // ---- Nokta grupları ----
        g.MapPost("/katmanlar", (KatmanIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (KatmanHatasi(i) is { } hata) return Hata(hata);
                var id = await db.TekDegerAsync<int>(KatmanEkle, KatmanParam(k.Id, i), ct);
                return Results.Ok(new { id, mesaj = "Nokta/POI grubu oluşturuldu." });
            }));

        g.MapPut("/katmanlar/{id:int}", (int id, KatmanIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (KatmanHatasi(i) is { } hata) return Hata(hata);
                var p = KatmanParam(k.Id, i); p["id"] = id;
                return Sonuc(await db.CalistirAsync(KatmanGuncelle, p, ct), "Nokta/POI grubu güncellendi.");
            }));

        g.MapDelete("/katmanlar/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
                Sonuc(await db.CalistirAsync(KatmanSil, new Dictionary<string, object?> { ["id"] = id, ["kullaniciId"] = k.Id }, ct), "Nokta/POI grubu silindi.")));

        // ---- Noktalar ----
        g.MapPost("/katmanlar/{katmanId:int}/noktalar", (int katmanId, NoktaIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (NoktaHatasi(i) is { } hata) return Hata(hata);
                var p = NoktaParam(k.Id, i); p["katmanId"] = katmanId;
                var id = await db.TekDegerAsync<int?>(NoktaEkle, p, ct);
                return id is null ? Bulunamadi() : Results.Ok(new { id, mesaj = "Yeni nokta oluşturuldu." });
            }));

        g.MapPut("/noktalar/{id:int}", (int id, NoktaIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (NoktaHatasi(i) is { } hata) return Hata(hata);
                var p = NoktaParam(k.Id, i); p["id"] = id;
                return Sonuc(await db.CalistirAsync(NoktaGuncelle, p, ct), "Nokta güncellendi.");
            }));

        g.MapDelete("/noktalar/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
                Sonuc(await db.CalistirAsync(NoktaSil, new Dictionary<string, object?> { ["id"] = id, ["kullaniciId"] = k.Id }, ct), "Nokta silindi.")));

        // ---- Bölgeler ----
        g.MapPost("/bolgeler", (SekilIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (SekilHatasi(i, 3, zorunlu: true) is { } hata) return Hata(hata);
                var id = await db.TekDegerAsync<int>(BolgeEkle, BolgeParam(k.Id, i), ct);
                return Results.Ok(new { id, mesaj = "Yeni bölge kaydedildi." });
            }));

        g.MapPut("/bolgeler/{id:int}", (int id, SekilIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (SekilHatasi(i, 3, zorunlu: false) is { } hata) return Hata(hata);
                var p = BolgeParam(k.Id, i); p["id"] = id;
                return Sonuc(await db.CalistirAsync(BolgeGuncelle, p, ct), "Bölge değişiklikleri kaydedildi.");
            }));

        g.MapDelete("/bolgeler/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
                Sonuc(await db.CalistirAsync(BolgeSil, new Dictionary<string, object?> { ["id"] = id, ["kullaniciId"] = k.Id }, ct), "Bölge silindi.")));

        // ---- Rotalar ----
        g.MapPost("/rotalar", (SekilIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (SekilHatasi(i, 2, zorunlu: true) is { } hata) return Hata(hata);
                var wkt = "LINESTRING(" + string.Join(", ", i.Noktalar!.Select(n => $"{S(n[1])} {S(n[0])}")) + ")";
                var id = await db.TekDegerAsync<int>(RotaEkle, new Dictionary<string, object?>
                {
                    ["kullaniciId"] = k.Id, ["ad"] = i.Ad!.Trim(), ["renk"] = RenkTemizle(i.Renk), ["wkt"] = wkt,
                }, ct);
                return Results.Ok(new { id, mesaj = "Rota bilgileri kaydedildi." });
            }));

        g.MapPut("/rotalar/{id:int}", (int id, SekilIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
            {
                if (string.IsNullOrWhiteSpace(i.Ad)) return Hata("Rota adı giriniz.");
                return Sonuc(await db.CalistirAsync(RotaGuncelle, new Dictionary<string, object?>
                {
                    ["id"] = id, ["kullaniciId"] = k.Id, ["ad"] = i.Ad.Trim(), ["renk"] = RenkTemizle(i.Renk),
                }, ct), "Rota güncellendi.");
            }));

        g.MapDelete("/rotalar/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Yaz(h, o, ct, async k =>
                Sonuc(await db.CalistirAsync(RotaSil, new Dictionary<string, object?> { ["id"] = id, ["kullaniciId"] = k.Id }, ct), "Rota silindi.")));
    }

    // ---- Yardımcılar ----

    private static async Task<IResult> Yaz(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }

    private static IResult Hata(string mesaj) => Results.BadRequest(new { error = mesaj });
    private static IResult Bulunamadi() => Results.NotFound(new { error = "Kayıt bulunamadı ya da size ait değil." });
    private static IResult Sonuc(int etkilenen, string mesaj) => etkilenen > 0 ? Results.Ok(new { mesaj }) : Bulunamadi();

    private static string? KatmanHatasi(KatmanIstegi i) =>
        string.IsNullOrWhiteSpace(i.Ad) ? "Grup adı giriniz."
        : i.Ad.Length > 100 ? "Grup adı çok uzun."
        : i.Boyut is < 1 or > 5 ? "Boyut 1-5 arasında olmalı."
        : !int.TryParse(i.Ikon, out var ik) || ik is < 0 or > 33 ? "Geçersiz ikon."
        : null;

    private static Dictionary<string, object?> KatmanParam(int kullaniciId, KatmanIstegi i) => new()
    {
        ["kullaniciId"] = kullaniciId, ["ad"] = i.Ad!.Trim(), ["ikon"] = i.Ikon, ["boyut"] = i.Boyut, ["paylas"] = i.Paylas ? 1 : 0,
    };

    private static string? NoktaHatasi(NoktaIstegi i) =>
        string.IsNullOrWhiteSpace(i.Baslik) ? "Nokta adı giriniz."
        : !Konum(i.Lat, i.Lon) ? "Geçersiz konum."
        : i.Cap is < 0 or > 100000 ? "Geçersiz çap."
        : null;

    private static Dictionary<string, object?> NoktaParam(int kullaniciId, NoktaIstegi i) => new()
    {
        ["kullaniciId"] = kullaniciId, ["baslik"] = i.Baslik!.Trim(), ["cap"] = i.Cap ?? 100, ["x"] = S(i.Lon), ["y"] = S(i.Lat),
    };

    private static string? SekilHatasi(SekilIstegi i, int enAz, bool zorunlu)
    {
        if (string.IsNullOrWhiteSpace(i.Ad)) return "Ad giriniz.";
        if (i.Noktalar is null || i.Noktalar.Length == 0) return zorunlu ? "Haritada şekli çiziniz." : null;
        if (i.Noktalar.Length < enAz) return $"En az {enAz} nokta gerekli.";
        if (i.Noktalar.Length > 2000) return "Çok fazla nokta.";
        return i.Noktalar.All(n => n.Length == 2 && Konum(n[0], n[1])) ? null : "Geçersiz konum.";
    }

    private static Dictionary<string, object?> BolgeParam(int kullaniciId, SekilIstegi i) => new()
    {
        ["kullaniciId"] = kullaniciId,
        ["ad"] = i.Ad!.Trim(),
        ["renk"] = RenkTemizle(i.Renk),
        ["noktalar"] = i.Noktalar is { Length: > 0 } n ? string.Concat(n.Select(p => $"{S(p[1])}x{S(p[0])}_")) : null,
    };

    private static bool Konum(double lat, double lon) => lat is >= -90 and <= 90 && lon is >= -180 and <= 180 && !(lat == 0 && lon == 0);

    private static string RenkTemizle(string? renk)
    {
        var r = (renk ?? "").Trim().TrimStart('#');
        return r.Length == 6 && r.All(Uri.IsHexDigit) ? r.ToUpperInvariant() : "2B8FD6";
    }

    private static string S(double d) => d.ToString("0.0#########", CultureInfo.InvariantCulture);
}
