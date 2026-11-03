using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Göstergeler (eski: dashboard.asp + data/_dashboard.asp), mesajlar (data/_messages.asp, _prcMessageReaded.asp,
/// _prcMessageDelete.asp), DVR listesi (eski menüdeki "DVR-Kamera") ve destek bilgisi (_destek.asp).
/// Fark: eski dashboard.asp her açılışta kullanıcının araçlarında cihazkomut 'SK' kayıtlarını STATUS=0 yapıyordu;
/// bu yan etki cihaz komut tablosuna yazdığı için burada yok.
/// </summary>
public static class GenelEndpoints
{
    private const string Araclarim = "SELECT CLIENTID FROM userclients WITH (NOLOCK) WHERE USERID = @kullaniciId";

    // Genel durum: son 25 dakikada sinyal verenler hız / kontağa göre, kalanlar "veri yok" (eski genelDurum).
    private const string GenelDurum = $"""
        SELECT
          SUM(CASE WHEN g.SINYAL = 1 AND c.CURRENTSPEED >= 5 AND c.KONTAK = 1 THEN 1 ELSE 0 END) AS HAREKETLI,
          SUM(CASE WHEN g.SINYAL = 1 AND c.CURRENTSPEED < 5 AND c.KONTAK = 1 THEN 1 ELSE 0 END) AS ROLANTI,
          SUM(CASE WHEN g.SINYAL = 1 AND c.CURRENTSPEED < 5 AND c.KONTAK = 0 THEN 1 ELSE 0 END) AS PARK,
          SUM(CASE WHEN g.SINYAL = 0 THEN 1 ELSE 0 END) AS VERIYOK,
          COUNT(*) AS TOPLAM
        FROM clients c WITH (NOLOCK)
        CROSS APPLY (SELECT CASE WHEN ABS(DATEDIFF(minute, c.KONTROLTARIH, GETDATE())) < 25 THEN 1 ELSE 0 END AS SINYAL) g
        WHERE c.STATUS = 1 AND c.ID IN ({Araclarim})
        """;

    // Bugünkü hız ihlali, rota dışı, bölge ihlali sayıları (eski alarmTablosu).
    private const string Alarmlar = $"""
        DECLARE @g1 datetime = CONVERT(date, GETDATE()), @g2 datetime = DATEADD(day, 1, CONVERT(date, GETDATE()));
        SELECT rt.RAPORKODU, rt.RAPORADI,
               (SELECT COUNT(*) FROM konumlar k WITH (NOLOCK)
                WHERE k.CIHAZID IN (SELECT CIHAZID FROM clients WITH (NOLOCK) WHERE STATUS = 1 AND ID IN ({Araclarim}))
                  AND k.TARIH >= @g1 AND k.TARIH < @g2 AND k.RAPORKODU = rt.RAPORKODU) AS ADET
        FROM raportanim rt WITH (NOLOCK)
        WHERE rt.RAPORKODU IN ('AH', 'XR', 'BA')
        ORDER BY rt.RAPORADI
        """;

    // Bugünkü mesafe (mesafe tablosu, metre): araç başına; mesai içi MESAI (-1 = mesai tanımsız → 0).
    private const string Mesafeler = $"""
        SELECT c.ID, c.PLAKA, m.MAXM - m.MINM AS MESAFE, CASE WHEN m.MESAI = -1 THEN 0 ELSE m.MESAI END AS MESAI
        FROM mesafe m WITH (NOLOCK)
        JOIN clients c WITH (NOLOCK) ON c.CIHAZID = m.CIHAZID AND c.STATUS = 1 AND c.ID IN ({Araclarim})
        WHERE m.TARIH = CONVERT(date, GETDATE())
        """;

    // Bugünkü hızlar: araç başına en yüksek (hareket kayıtları VZ, hız ≥ 5), filo ortalaması (eski hizTablosu).
    private const string Hizlar = $"""
        DECLARE @g1 datetime = CONVERT(date, GETDATE()), @g2 datetime = DATEADD(day, 1, CONVERT(date, GETDATE()));
        SELECT c.ID, c.PLAKA, MAX(k.HIZ) AS MAKS, SUM(CAST(k.HIZ AS bigint)) AS TOPLAM, COUNT(*) AS ADET
        FROM clients c WITH (NOLOCK)
        JOIN konumlar k WITH (NOLOCK) ON k.CIHAZID = c.CIHAZID AND k.TARIH >= @g1 AND k.TARIH < @g2 AND k.RAPORKODU = 'VZ' AND k.HIZ >= 5
        WHERE c.STATUS = 1 AND c.ID IN ({Araclarim})
        GROUP BY c.ID, c.PLAKA
        """;

    private const string MesajListesi = """
        SELECT m.ID, m.TARIH, u.ADI_SOYADI, u.USERNAME, m.BASLIK, m.MESAJ, m.DURUM
        FROM mesajlar m WITH (NOLOCK) LEFT JOIN users u WITH (NOLOCK) ON u.ID = m.KIMDEN
        WHERE m.KIME = @kullaniciId
        ORDER BY m.TARIH DESC
        """;

    private const string MesajOkundu = "UPDATE mesajlar SET DURUM = 1 WHERE ID = @id AND KIME = @kullaniciId";
    private const string MesajSil = "DELETE FROM mesajlar WHERE ID = @id AND KIME = @kullaniciId";

    private const string DvrAraclari = $"""
        SELECT c.ID, c.PLAKA, LTRIM(RTRIM(c.DVRIPNO)) AS DVR
        FROM clients c WITH (NOLOCK)
        WHERE c.STATUS = 1 AND c.ID IN ({Araclarim}) AND ISNULL(LTRIM(RTRIM(c.DVRIPNO)), '') NOT IN ('', '1.1.1.1')
        ORDER BY c.PLAKA
        """;

    // Eski conn3.asp bayi kodunu sabit 'karmobil' alıyordu.
    private const string Bayi = "SELECT TOP 1 TITLE, DESTEKTEL, DESTEKWEB FROM bayiler WITH (NOLOCK) WHERE STATUS = '1' AND BID = 'karmobil'";

    public static void MapGenel(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").RequireAuthorization();

        g.MapGet("/gostergeler", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var p = P(k);
            var durumT = db.TekSatirAsync(GenelDurum, p, ct);
            var alarmT = db.SorguAsync(Alarmlar, p, ct);
            var mesafeT = db.SorguAsync(Mesafeler, p, ct);
            var hizT = db.SorguAsync(Hizlar, p, ct);
            await Task.WhenAll(durumT, alarmT, mesafeT, hizT);

            var d = durumT.Result;
            int Say(string ad) => d?[ad] is null ? 0 : Convert.ToInt32(d[ad]);

            // Sayaç sıfırlanmış / bozuk günlerde fark eksi olabilir; 0 sayılır.
            var mesafe = mesafeT.Result.Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                plaka = RaporYardimci.Metin(s["PLAKA"]),
                km = Math.Max(0, (RaporYardimci.Ondalik(s["MESAFE"]) ?? 0) / 1000),
                mesaiKm = Math.Max(0, (RaporYardimci.Ondalik(s["MESAI"]) ?? 0) / 1000),
            }).ToList();
            var hiz = hizT.Result.Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                plaka = RaporYardimci.Metin(s["PLAKA"]),
                maks = Convert.ToInt32(s["MAKS"]),
                toplam = Convert.ToInt64(s["TOPLAM"]),
                adet = Convert.ToInt64(s["ADET"]),
            }).ToList();
            var toplamKm = mesafe.Sum(m => m.km);
            var mesaiIci = Math.Min(toplamKm, mesafe.Sum(m => Math.Min(m.mesaiKm, m.km)));

            return Results.Ok(new
            {
                durum = new { hareketli = Say("HAREKETLI"), rolanti = Say("ROLANTI"), park = Say("PARK"), veriYok = Say("VERIYOK"), toplam = Say("TOPLAM") },
                alarmlar = alarmT.Result.Select(s => new { kod = (s["RAPORKODU"] as string ?? "").Trim(), ad = RaporYardimci.Metin(s["RAPORADI"]), adet = Convert.ToInt32(s["ADET"]) }),
                mesafe = new
                {
                    toplamKm = Math.Round(toplamKm, 1),
                    ortalamaKm = mesafe.Count > 0 ? Math.Round(toplamKm / mesafe.Count, 1) : 0,
                    mesaiIciKm = Math.Round(mesaiIci, 1),
                    mesaiDisiKm = Math.Round(toplamKm - mesaiIci, 1),
                    enCok = mesafe.Where(m => m.km > 0).OrderByDescending(m => m.km).Take(5).Select(m => new { m.id, m.plaka, deger = Math.Round(m.km, 1) }),
                },
                hiz = new
                {
                    maks = hiz.Count > 0 ? hiz.Max(x => x.maks) : 0,
                    ortalama = hiz.Sum(x => x.adet) is > 0 and var n ? Math.Round((double)hiz.Sum(x => x.toplam) / n) : 0,
                    enCok = hiz.OrderByDescending(x => x.maks).Take(5).Select(x => new { x.id, x.plaka, deger = x.maks }),
                },
            });
        }));

        g.MapGet("/mesajlar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(MesajListesi, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt64(s["ID"]),
                tarih = s["TARIH"],
                kimden = RaporYardimci.Metin(s["ADI_SOYADI"]) ?? RaporYardimci.Metin(s["USERNAME"]),
                baslik = RaporYardimci.Metin(s["BASLIK"]),
                mesaj = RaporYardimci.Metin(s["MESAJ"]),
                okundu = s["DURUM"] is not null && Convert.ToInt32(s["DURUM"]) == 1,
            }))));

        g.MapPost("/mesajlar/{id:long}/okundu", (long id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            await db.CalistirAsync(MesajOkundu, new Dictionary<string, object?>(P(k)) { ["id"] = id }, ct) > 0
                ? Results.Ok(new { mesaj = "Okundu." }) : Results.NotFound(new { error = "Mesaj bulunamadı." })));

        g.MapDelete("/mesajlar/{id:long}", (long id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            await db.CalistirAsync(MesajSil, new Dictionary<string, object?>(P(k)) { ["id"] = id }, ct) > 0
                ? Results.Ok(new { mesaj = "Mesaj silindi." }) : Results.NotFound(new { error = "Mesaj bulunamadı." })));

        g.MapGet("/dvr", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(DvrAraclari, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                plaka = RaporYardimci.Metin(s["PLAKA"]),
                adres = s["DVR"] as string,
            }))));

        g.MapGet("/destek", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            var b = await db.TekSatirAsync(Bayi, null, ct);
            return Results.Ok(new
            {
                unvan = RaporYardimci.Metin(b?["TITLE"]),
                telefon = RaporYardimci.Metin(b?["DESTEKTEL"]),
                web = RaporYardimci.Metin(b?["DESTEKWEB"]),
                // Eski _destek.asp'de sabit yazılıydı.
                whatsapp = "0 506 600 32 02",
            });
        }));
    }

    private static Dictionary<string, object?> P(OturumKullanici k) => new() { ["kullaniciId"] = k.Id };

    private static async Task<IResult> Kullanici(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }
}
