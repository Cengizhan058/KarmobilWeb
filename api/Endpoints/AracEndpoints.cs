using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Models;

namespace Karmobil.Api.Endpoints;

public static class AracEndpoints
{
    public static void MapArac(this IEndpointRouteBuilder app)
    {
        var grup = app.MapGroup("/api").WithTags("Araçlar").RequireAuthorization();

        // Haritadaki araçlar ve listesi; arayüz bunu birkaç saniyede bir yeniden çeker.
        grup.MapGet("/araclar", async (int? grupId, HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();

            var satirlar = await db.SorguAsync(
                Sql.AracKonumlari,
                new Dictionary<string, object?> { ["kullaniciId"] = k.Id, ["grupId"] = grupId },
                ct);

            return Results.Ok(satirlar.Select(AracDto).ToList());
        });

        // Araç bilgi paneli: kimlik/ruhsat özet bilgileri + bugünkü mesafe, son kontak, son alarm.
        grup.MapGet("/araclar/{aracId:int}/detay", async (int aracId, HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();

            var s = await db.TekSatirAsync(Sql.AracDetay, new Dictionary<string, object?> { ["aracId"] = aracId, ["kullaniciId"] = k.Id }, ct);
            if (s is null) return Results.NotFound(new { error = "Araç bulunamadı." });

            var cihazId = (s["CIHAZID"] as string ?? "").Trim();
            var simdi = DateTime.Now;
            var p = new Dictionary<string, object?>
            {
                ["cihazId"] = cihazId,
                ["ilk"] = simdi.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                ["son"] = simdi.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            };
            var mesafeMetre = Ondalik(await db.TekDegerAsync<object>(Sql.GunlukMesafe, p, ct));

            var kontakAcik = (s["KONTAK"]?.ToString() ?? "0").Trim() == "1";
            var sonDurus = s["LASTSTOP"] as DateTime?;

            // Kontak kapalıysa: son duruş + o duruştan önceki son kontak açma; açıksa: son kontak açma.
            var acma = await db.TekSatirAsync(Sql.SonKontakAcma,
                new Dictionary<string, object?> { ["cihazId"] = cihazId, ["sinir"] = kontakAcik ? null : sonDurus }, ct);
            var acmaTarihi = acma?["TARIH"] as DateTime?;

            var alarm = await db.TekSatirAsync(Sql.SonAlarm, new Dictionary<string, object?> { ["cihazId"] = cihazId }, ct);

            var dvr = (s["DVRIPNO"] as string)?.Trim();
            var batarya = Ondalik(s["SKBATTERY"]);
            var motor = (s["MOTOR"]?.ToString() ?? "").Trim();

            return Results.Ok(new AracDetayDto(
                Id: Tam(s["ID"]),
                CihazId: cihazId,
                Plaka: (s["PLAKA"] as string ?? "").Trim(),
                CihazTanimi: string.Join('-', new[] { BosIseNull(s["CIHAZTIPI"] as string), cihazId, Baglanti(s) }.Where(x => !string.IsNullOrEmpty(x))),
                Gsm: BosIseNull(s["GSM"] as string),
                Imei: BosIseNull(s["IMEINO"] as string),
                AracSahibi: BosIseNull(s["ARACSAHIBI"] as string),
                AracCinsi: BosIseNull(s["ARACCINSI"] as string),
                AracMarkasi: BosIseNull(s["ARACMARKASI"] as string),
                AracTipi: BosIseNull(s["ARACTIPI"] as string),
                Aciklama: BosIseNull(s["ACIKLAMA"] as string),
                DvrAdresi: !string.IsNullOrEmpty(dvr) && dvr != "1.1.1.1" ? dvr : null,
                // _infoPanell.asp: batarya yalnızca 0-100 arasıysa anlamlı (pilli cihazlar).
                Batarya: batarya is >= 0 and <= 100 ? batarya : null,
                // MOTOR: 0 bloke pasif, 1 bloke aktif, 2 blokaj yok (images/bloke/g-*.png).
                MotorBlokaj: motor switch { "0" => "pasif", "1" => "aktif", _ => null },
                AzamiHiz: Ondalik(s["MAXHIZ"]),
                BugunKm: mesafeMetre is null ? null : Math.Round(mesafeMetre.Value / 1000, 1),
                KontakAcik: kontakAcik,
                SonKontakKapama: kontakAcik ? null : sonDurus,
                SonKontakKapamaAdresi: kontakAcik ? null : MetinTemizle(BosIseNull(s["SKKONUM"] as string)),
                ParkSuresiDakika: kontakAcik || sonDurus is null ? null : Tam(s["PARK_DAKIKA"]),
                SonKontakAcma: acmaTarihi,
                SonKontakAcmaAdresi: MetinTemizle(BosIseNull(acma?["KONUM"] as string)),
                KontakAcikSureDakika: acmaTarihi is null ? null
                    : (int)Math.Abs(((kontakAcik ? simdi : sonDurus ?? simdi) - acmaTarihi.Value).TotalMinutes),
                SonAlarm: BosIseNull(alarm?["ALARM"] as string),
                SonAlarmTarihi: alarm?["TARIH"] as DateTime?,
                SonAlarmAdresi: MetinTemizle(BosIseNull(alarm?["KONUM"] as string)),
                IlkSinyal: s["ILKSINYALTARIH"] as DateTime?,
                KayitTarihi: s["KAYITTARIHI"] as DateTime?));
        });

        // Anlık konum isteği: eski sitedeki data/_prcAnlikKonum.asp ile aynı yazma (cihazkomut, KOMUT='SK').
        // Gateway bu kaydı okuyup cihaza konum gönder komutu yollar. Yalnızca kullanıcının kendi aracı için.
        grup.MapPost("/araclar/{aracId:int}/anlik-konum", async (int aracId, HttpContext http, OturumServisi oturum, SqlDb db, ILogger<SqlDb> log, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();

            var s = await db.TekSatirAsync(Sql.AracDetay, new Dictionary<string, object?> { ["aracId"] = aracId, ["kullaniciId"] = k.Id }, ct);
            if (s is null) return Results.NotFound(new { error = "Araç bulunamadı." });

            var cihazId = (s["CIHAZID"] as string ?? "").Trim();
            var yazildi = await db.TekDegerAsync<int>(Sql.AnlikKonumIste, new Dictionary<string, object?> { ["cihazId"] = cihazId }, ct);
            log.LogInformation("Anlık konum isteği: kullanıcı {Kullanici}, cihaz {Cihaz}, yazıldı={Yazildi}", k.Id, cihazId, yazildi);

            return yazildi == 1
                ? Results.Ok(new { mesaj = "Anlık konum isteği gönderildi." })
                : Results.BadRequest(new { error = "Cihaz şu an komut kabul etmiyor (bağlantı yok veya arızalı)." });
        });

        grup.MapGet("/gruplar",async (HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();

            var satirlar = await db.SorguAsync(Sql.Gruplar, new Dictionary<string, object?> { ["kullaniciId"] = k.Id }, ct);
            return Results.Ok(satirlar.Select(s => new GrupDto(
                Tam(s["ID"]), (s["GROUPNAME"] as string ?? "").Trim(), s["PARENTID"] is null ? null : Tam(s["PARENTID"]))).ToList());
        });

        grup.MapGet("/mesajlar/okunmamis", async (HttpContext http, OturumServisi oturum, SqlDb db, CancellationToken ct) =>
        {
            var k = await oturum.KullaniciAlAsync(http.User, ct);
            if (k is null) return Results.Unauthorized();
            var sayi = await db.TekDegerAsync<int>(Sql.OkunmamisMesajSayisi, new Dictionary<string, object?> { ["kullaniciId"] = k.Id.ToString() }, ct);
            return Results.Ok(new { sayi });
        });
    }

    private static AracKonumDto AracDto(Dictionary<string, object?> s)
    {
        var dvr = (s["DVRIPNO"] as string)?.Trim();
        return new AracKonumDto(
            Id: Tam(s["ID"]),
            CihazId: (s["CIHAZID"] as string ?? "").Trim(),
            Plaka: (s["PLAKA"] as string ?? "").Trim(),
            Durum: (string)s["DURUM"]!,
            Kontak: (s["KONTAK"]?.ToString() ?? "0").Trim() == "1",
            Lat: Ondalik(s["LAT"]),
            Lon: Ondalik(s["LON"]),
            Hiz: Ondalik(s["HIZ"]) ?? 0,
            Yon: Ondalik(s["YON"]) ?? 0,
            GpsHatasi: (s["GPSHATA"]?.ToString() ?? "0").Trim() != "0",
            Batarya: s["BATARYA"]?.ToString()?.Trim(),
            Isi: s["ISI"]?.ToString()?.Trim(),
            Surucu: BosIseNull(s["SURUCU"] as string),
            GpsTarihi: s["GPS_TARIH"] as DateTime?,
            SinyalTarihi: s["SINYAL_TARIH"] as DateTime?,
            Adres: MetinTemizle(BosIseNull(s["ADRES"] as string)),
            CihazTipi: BosIseNull(s["CIHAZTIPI"] as string),
            GrupIdleri: GrupListesi(s["FILO_IDS"] as string),
            Dvr: !string.IsNullOrEmpty(dvr) && dvr != "1.1.1.1",
            MotorBlokeli: (s["MOTOR"]?.ToString() ?? "0").Trim() == "1",
            GunlukKm: Ondalik(s["GUNLUK_KM"]),
            KadranKm: Ondalik(s["KADRAN_KM"]));
    }

    // _jsondata.asp CONNTYPE kuralı: ilk harf + (APN / INT).
    private static string? Baglanti(Dictionary<string, object?> s)
    {
        var tur = (s["CONNTYPE"] as string ?? "").Trim();
        if (tur.Length == 0) return null;
        var server = (s["SERVERIP"] as string ?? "").Trim();
        var ip = (s["IPNO"] as string ?? "").Trim();
        var ek = server.StartsWith("10.") ? "-APN" : ip != "1.1.1.1" && !ip.StartsWith("10.") ? "-INT" : "";
        return tur[..1] + ek;
    }

    private static int Tam(object? v) => Convert.ToInt32(v, CultureInfo.InvariantCulture);

    private static double? Ondalik(object? v)
    {
        if (v is null) return null;
        if (v is string str)
            return double.TryParse(str.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
        return Convert.ToDouble(v, CultureInfo.InvariantCulture);
    }

    private static string? BosIseNull(string? v)
    {
        var t = v?.Trim();
        return string.IsNullOrEmpty(t) || t == "-" ? null : t;
    }

    // SKKONUM'da yanlış kodlamadan kalma C1 kontrol karakterleri var (ör. "Ş" + U+009E → "Ş☐ehitkamil").
    private static string? MetinTemizle(string? v) =>
        v is null ? null : System.Text.RegularExpressions.Regex.Replace(v.Normalize(), "[\u0080-\u009F​-‏﻿]", "");

    private static List<int> GrupListesi(string? v) =>
        (v ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
            .Where(x => x.HasValue).Select(x => x!.Value).ToList();
}
