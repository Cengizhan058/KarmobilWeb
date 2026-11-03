using System.Globalization;
using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Raporlar;

namespace Karmobil.Api.Endpoints;

/// <summary>
/// Araçlar menüsü: araç yönetimi (eski data/_getClientDetail.asp + _prcClients.asp) ve grup/filo yönetimi
/// (_getFleets.asp + _prcFleets.asp), araç listesi ve arızalı/montajsız araçlar.
///
/// Araç kaydında yalnız eski formun düzenlediği alanlara yazılır. CIHAZID, GSM, CIHAZTIPI, IMEI gibi
/// gateway'in kullandığı alanlar eski formda da salt okunurdu; burada hiç yazılmaz.
/// Yazma yetkisi: araç için users.YA (AracYonetimi), grup için YG (GrupYonetimi); araç userclients ile sahiplenilmiş olmalı.
/// Eski sitedeki açıklar kapatıldı: grup güncelleme sahibi denetlemiyordu, gruba başka kullanıcının aracı eklenebiliyordu.
/// </summary>
public static class YonetimEndpoints
{
    // Telefonla takip (CIHAZTIPI = 'GSM') eski araç yönetimi listesinde yoktu.
    private const string YonetimAraclari = """
        SELECT c.ID, c.CIHAZID, c.PLAKA, c.CIHAZTIPI, c.ICON
        FROM clients c WITH (NOLOCK) JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND ISNULL(c.CIHAZTIPI, '') <> 'GSM'
        ORDER BY c.PLAKA
        """;

    private const string AracKaydi = """
        SELECT c.ID, c.CIHAZID, c.GSM, c.CIHAZTIPI, c.PLAKA, c.ARACSAHIBI, c.ARACCINSI, c.ARACMARKASI, c.ARACTIPI, c.SOFOR, c.EHLIYETNO,
               c.ICON, c.MTARIH1, c.MTARIH2, c.STARIH1, c.STARIH2, c.KTARIH1, c.KTARIH2, c.ETARIH1, c.ETARIH2,
               dbo.kadranKm(c.CIHAZID) AS KADRAN_KM, c.KADRAN_KMTARIH, c.MOTOR_KM, c.LASTIK_KM, c.SKMESAFE,
               u.MCHECK, u.SCHECK, u.KCHECK, u.ECHECK, u.MOTOR AS MOTORCHECK, u.LASTIK AS LASTIKCHECK
        FROM clients c WITH (NOLOCK)
        JOIN userclients uc WITH (NOLOCK) ON uc.CLIENTID = c.ID AND uc.USERID = @kullaniciId
        LEFT JOIN uyarilar u WITH (NOLOCK) ON u.CIHAZID = c.CIHAZID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND c.ID = @id
        """;

    // _prcClients.asp "clsave" ile aynı: araç alanları, uyarı seçimleri (uyarilar) ve kadran başlangıcı (kadran).
    // Boş tarih mevcut değeri korur (eski sitede tüm kayıtlarda tarih dolu). Motor/lastik "kaç km sonra" verilirse
    // hedef = şimdiki toplam mesafe (SKMESAFE, metre) + km * 1000.
    private const string AracGuncelle = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @cihazId nvarchar(64) = (SELECT c.CIHAZID FROM clients c JOIN userclients u ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId WHERE c.ID = @id AND c.STATUS = 1);
        IF @cihazId IS NULL BEGIN ROLLBACK; SELECT 0; RETURN; END

        UPDATE clients SET
            PLAKA = @plaka,
            -- Okurken "-" boş gösterilir; boş bırakılan alan önceden "-" ise öyle kalır (eski veriyle aynı).
            ARACSAHIBI = CASE WHEN @aracSahibi = '' AND ARACSAHIBI = '-' THEN '-' ELSE @aracSahibi END, ARACCINSI = CASE WHEN @aracCinsi = '' AND ARACCINSI = '-' THEN '-' ELSE @aracCinsi END,
            ARACMARKASI = CASE WHEN @aracMarkasi = '' AND ARACMARKASI = '-' THEN '-' ELSE @aracMarkasi END, ARACTIPI = CASE WHEN @aracTipi = '' AND ARACTIPI = '-' THEN '-' ELSE @aracTipi END,
            SOFOR = CASE WHEN @surucu = '' AND SOFOR = '-' THEN '-' ELSE @surucu END, EHLIYETNO = CASE WHEN @aciklama = '' AND EHLIYETNO = '-' THEN '-' ELSE @aciklama END,
            ICON = @ikon,
            MTARIH1 = COALESCE(@m1, MTARIH1), MTARIH2 = COALESCE(@m2, MTARIH2),
            STARIH1 = COALESCE(@s1, STARIH1), STARIH2 = COALESCE(@s2, STARIH2),
            KTARIH1 = COALESCE(@k1, KTARIH1), KTARIH2 = COALESCE(@k2, KTARIH2),
            ETARIH1 = COALESCE(@e1, ETARIH1), ETARIH2 = COALESCE(@e2, ETARIH2),
            KADRAN_KM = @kadranKm, KADRAN_KMTARIH = GETDATE(),
            MOTOR_KM = CASE WHEN @motorKm IS NULL THEN MOTOR_KM ELSE SKMESAFE + @motorKm * 1000 END,
            LASTIK_KM = CASE WHEN @lastikKm IS NULL THEN LASTIK_KM ELSE SKMESAFE + @lastikKm * 1000 END
        WHERE ID = @id;

        DELETE FROM uyarilar WHERE USERID = @kullaniciId AND CIHAZID = @cihazId;
        INSERT INTO uyarilar (USERID, CIHAZID, MCHECK, SCHECK, KCHECK, ECHECK, MOTOR, LASTIK)
        VALUES (@kullaniciId, @cihazId, @uM, @uS, @uK, @uE, @uMotor, @uLastik);

        IF EXISTS (SELECT 1 FROM kadran WHERE CIHAZID = @cihazId)
            UPDATE kadran SET MESAFE = (SELECT TOP 1 SKMESAFE FROM clients WITH (NOLOCK) WHERE CIHAZID = @cihazId) WHERE CIHAZID = @cihazId;
        ELSE
            INSERT INTO kadran (CIHAZID, MESAFE) SELECT TOP 1 CIHAZID, SKMESAFE FROM clients WITH (NOLOCK) WHERE CIHAZID = @cihazId;
        COMMIT;
        SELECT 1;
        """;

    private const string Ikonlar = "SELECT ICONNO, ICONNAME FROM icons WITH (NOLOCK) ORDER BY ICONNO";

    // ---- Gruplar (filolar) ----
    private const string GrupListesi = """
        SELECT g.ID, g.GROUPNAME, g.PARENTID,
               (SELECT STUFF((SELECT ',' + CAST(m.CLIENTID AS varchar(12)) FROM groupmembers m WITH (NOLOCK) WHERE m.GROUPID = g.ID FOR XML PATH('')), 1, 1, '')) AS UYELER
        FROM groups g WITH (NOLOCK) WHERE g.USERID = @kullaniciId ORDER BY g.GROUPNAME
        """;

    // @uyeler: virgülle ayrılmış araç kimlikleri (yalnız rakam, C# tarafında int'ten üretilir; SQL 2012'de STRING_SPLIT yok).
    // Üyeler: yalnız kullanıcının erişebildiği araçlar (userclients ∪ usermobiles) eklenir; üst grup kullanıcının kendi grubu olmalı.
    private const string GrupKaydet = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        IF @ustId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM groups WHERE ID = @ustId AND USERID = @kullaniciId) SET @ustId = NULL;
        IF EXISTS (SELECT 1 FROM groups WHERE USERID = @kullaniciId AND GROUPNAME = @ad AND (@id IS NULL OR ID <> @id))
            BEGIN ROLLBACK; SELECT -1; RETURN; END
        IF @id IS NULL
        BEGIN
            INSERT INTO groups (GROUPNAME, USERID, PARENTID) VALUES (@ad, @kullaniciId, ISNULL(@ustId, 0));
            SET @id = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE groups SET GROUPNAME = @ad, PARENTID = ISNULL(@ustId, 0) WHERE ID = @id AND USERID = @kullaniciId;
            IF @@ROWCOUNT = 0 BEGIN ROLLBACK; SELECT 0; RETURN; END
        END
        DELETE FROM groupmembers WHERE GROUPID = @id;
        INSERT INTO groupmembers (GROUPID, CLIENTID)
        SELECT DISTINCT @id, v.CLIENTID
        FROM (SELECT n.x.value('.', 'int') AS CLIENTID
              FROM (SELECT CAST('<a>' + REPLACE(@uyeler, ',', '</a><a>') + '</a>' AS xml) AS d) t CROSS APPLY t.d.nodes('/a') n(x)) v
        WHERE v.CLIENTID IN (SELECT CLIENTID FROM userclients WHERE USERID = @kullaniciId UNION SELECT CLIENTID FROM usermobiles WHERE USERID = @kullaniciId);
        COMMIT;
        SELECT @id;
        """;

    private const string GrupSil = """
        DELETE FROM groupmembers WHERE GROUPID IN (SELECT ID FROM groups WHERE ID = @id AND USERID = @kullaniciId);
        DELETE FROM groups WHERE ID = @id AND USERID = @kullaniciId;
        """;

    // ---- Listeler (salt okuma) ----
    // Arızalı/montajsız: eski _getClientsPasifArizaAraclar.asp (dbo.AktifMi(KONTROLTARIH) = 0).
    private const string ArizaliAraclar = """
        SELECT c.ID, c.CIHAZID, c.PLAKA, c.KONTROLTARIH, c.EHLIYETNO, c.CIHAZTIPI, c.ILKSINYALTARIH
        FROM clients c WITH (NOLOCK) JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND dbo.AktifMi(c.KONTROLTARIH) = 0
        ORDER BY c.PLAKA
        """;

    // Aktif bildirimler: eski _getAlarmTable.asp (cihazalarm NUM1 > 0). Bölge alarmında (BA) NUM1 1 giriş, 2 çıkış, 3 ikisi; NUM2 bölge kimliği.
    private const string AktifBildirimler = """
        SELECT a.ID, c.PLAKA, a.KOMUT, a.NUM1, a.STR1, a.STR2, r.RAPORADI, b.REGION_NAME
        FROM cihazalarm a WITH (NOLOCK)
        JOIN clients c WITH (NOLOCK) ON c.CIHAZID = a.CIHAZID AND c.STATUS = 1
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        LEFT JOIN raportanim r WITH (NOLOCK) ON r.RAPORKODU = a.KOMUT
        LEFT JOIN regions b WITH (NOLOCK) ON a.KOMUT = 'BA' AND b.ID = a.NUM2
        WHERE a.NUM1 > 0
        ORDER BY c.PLAKA, r.RAPORADI
        """;

    // Mevcut program tablosu: eski _getClientsProgramTable.asp. Her komut için cihazkomut'taki son kayıt;
    // kayıt yoksa "varsayılan", NUM1 0 kapalı, 1 açık (değer PZ/PM/PH için NUM2, PD için NUM4).
    private const string ProgramTablosu = """
        SELECT c.ID, c.PLAKA, c.CONNTYPE,
               pz.NUM1 AS PZ1, pz.NUM2 AS PZ, pd.NUM1 AS PD1, pd.NUM4 AS PD, pm.NUM1 AS PM1, pm.NUM2 AS PM, ph.NUM1 AS PH1, ph.NUM2 AS PH
        FROM clients c WITH (NOLOCK)
        JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        OUTER APPLY (SELECT TOP 1 NUM1, NUM2 FROM cihazkomut WITH (NOLOCK) WHERE CIHAZID = c.CIHAZID AND KOMUT = 'PZ') pz
        OUTER APPLY (SELECT TOP 1 NUM1, NUM4 FROM cihazkomut WITH (NOLOCK) WHERE CIHAZID = c.CIHAZID AND KOMUT = 'PD') pd
        OUTER APPLY (SELECT TOP 1 NUM1, NUM2 FROM cihazkomut WITH (NOLOCK) WHERE CIHAZID = c.CIHAZID AND KOMUT = 'PM') pm
        OUTER APPLY (SELECT TOP 1 NUM1, NUM2 FROM cihazkomut WITH (NOLOCK) WHERE CIHAZID = c.CIHAZID AND KOMUT = 'PH') ph
        WHERE c.STATUS = 1
        ORDER BY c.PLAKA
        """;

    // Motor blokesi aktif araçlar: eski _getClientsBlokeliAraclar.asp (clients.MOTOR = 1).
    private const string BlokeliAraclar = """
        SELECT c.ID, c.CIHAZID, c.GSM, c.PLAKA
        FROM clients c WITH (NOLOCK) JOIN userclients u WITH (NOLOCK) ON u.CLIENTID = c.ID AND u.USERID = @kullaniciId
        WHERE c.STATUS = 1 AND c.MOTOR = 1
        ORDER BY c.PLAKA
        """;

    public sealed record AracIstegi(
        string? Plaka, string? AracSahibi, string? AracCinsi, string? AracMarkasi, string? AracTipi, string? Surucu, string? Aciklama,
        int Ikon, DateTime? Muayene1, DateTime? Muayene2, DateTime? Sigorta1, DateTime? Sigorta2, DateTime? Kasko1, DateTime? Kasko2,
        DateTime? Egzoz1, DateTime? Egzoz2, bool MuayeneUyari, bool SigortaUyari, bool KaskoUyari, bool EgzozUyari,
        long KadranKm, double? MotorKm, bool MotorUyari, double? LastikKm, bool LastikUyari);

    public sealed record GrupIstegi(string? Ad, int? UstId, int[]? Araclar);

    public static void MapYonetim(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/yonetim").WithTags("Yönetim").RequireAuthorization();

        // ---- Salt okunur listeler (Ayarlar menüsü) ----
        g.MapGet("/aktif-bildirimler", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(AktifBildirimler, P(k), ct)).Select(s =>
            {
                var komut = Metin(s["KOMUT"]);
                var num1 = Ondalik(s["NUM1"]);
                var durum = komut != "BA" ? "" : (num1 switch { 1 => "Giriş: ", 2 => "Çıkış: ", 3 => "Giriş ve çıkış: ", _ => "" }) + (Metin(s["REGION_NAME"]) ?? "");
                return new
                {
                    id = Convert.ToInt32(s["ID"]),
                    plaka = Metin(s["PLAKA"]),
                    alarm = Metin(s["RAPORADI"]) ?? komut,
                    durum,
                    // Eski görünüm: STR1/STR2 saat (ör. "8" → "08:00").
                    saatAraligi = $"{Saat(s["STR1"])} - {Saat(s["STR2"])}",
                };
            }))));

        g.MapGet("/program-tablosu", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.CihazProgramlama) return Yasak();
            static string Durum(object? acik, object? deger) => Ondalik(acik) switch
            {
                null or -1 => "Varsayılan",
                0 => "Kapalı",
                1 => Ondalik(deger)?.ToString(CultureInfo.InvariantCulture) ?? "",
                _ => "Programlanmamış",
            };
            return Results.Ok((await db.SorguAsync(ProgramTablosu, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                plaka = Metin(s["PLAKA"]),
                hareketliSn = Durum(s["PZ1"], s["PZ"]),
                dururkenSn = Durum(s["PD1"], s["PD"]),
                mesafeKm = Durum(s["PM1"], s["PM"]),
                hizLimiti = Durum(s["PH1"], s["PH"]),
                baglanti = Metin(s["CONNTYPE"]),
            }));
        }));

        g.MapGet("/blokeli-araclar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.CihazProgramlama) return Yasak();
            return Results.Ok((await db.SorguAsync(BlokeliAraclar, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]), cihazId = Metin(s["CIHAZID"]), gsm = Metin(s["GSM"]), plaka = Metin(s["PLAKA"]),
            }));
        }));

        g.MapGet("/arac-ikonlari", async (SqlDb db, CancellationToken ct) =>
            Results.Ok((await db.SorguAsync(Ikonlar, null, ct)).Select(s => new { no = Convert.ToInt32(s["ICONNO"]), ad = RaporYardimci.Metin(s["ICONNAME"]) })));

        // ---- Araç yönetimi ----
        g.MapGet("/araclar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AracYonetimi) return Yasak();
            return Results.Ok((await db.SorguAsync(YonetimAraclari, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                cihazId = Metin(s["CIHAZID"]),
                plaka = Metin(s["PLAKA"]),
                cihazTipi = Metin(s["CIHAZTIPI"]),
                ikon = s["ICON"] is null ? 1 : Convert.ToInt32(s["ICON"]),
            }));
        }));

        g.MapGet("/araclar/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AracYonetimi) return Yasak();
            var p = P(k); p["id"] = id;
            var s = await db.TekSatirAsync(AracKaydi, p, ct);
            if (s is null) return Results.NotFound(new { error = "Araç bulunamadı." });
            var mesafe = Ondalik(s["SKMESAFE"]) ?? 0;
            // Eski _getClientDetail.asp: kalan km = (hedef - şimdiki) / 1000; hedef 0 ise boş.
            double? Kalan(object? hedef) => Ondalik(hedef) is { } h && h > 0 ? Math.Round((h - mesafe) / 1000) : null;
            return Results.Ok(new
            {
                id = Convert.ToInt32(s["ID"]),
                cihazId = Metin(s["CIHAZID"]),
                gsm = Metin(s["GSM"]),
                cihazTipi = Metin(s["CIHAZTIPI"]),
                plaka = Metin(s["PLAKA"]),
                aracSahibi = Metin(s["ARACSAHIBI"]),
                aracCinsi = Metin(s["ARACCINSI"]),
                aracMarkasi = Metin(s["ARACMARKASI"]),
                aracTipi = Metin(s["ARACTIPI"]),
                surucu = Metin(s["SOFOR"]),
                aciklama = Metin(s["EHLIYETNO"]),
                ikon = s["ICON"] is null ? 1 : Convert.ToInt32(s["ICON"]),
                muayene1 = Gun(s["MTARIH1"]), muayene2 = Gun(s["MTARIH2"]),
                sigorta1 = Gun(s["STARIH1"]), sigorta2 = Gun(s["STARIH2"]),
                kasko1 = Gun(s["KTARIH1"]), kasko2 = Gun(s["KTARIH2"]),
                egzoz1 = Gun(s["ETARIH1"]), egzoz2 = Gun(s["ETARIH2"]),
                muayeneUyari = Isaret(s["MCHECK"]), sigortaUyari = Isaret(s["SCHECK"]),
                kaskoUyari = Isaret(s["KCHECK"]), egzozUyari = Isaret(s["ECHECK"]),
                kadranKm = s["KADRAN_KM"] is null ? 0L : Convert.ToInt64(s["KADRAN_KM"]),
                kadranTarihi = s["KADRAN_KMTARIH"] as DateTime?,
                motorKm = Kalan(s["MOTOR_KM"]), motorUyari = Isaret(s["MOTORCHECK"]),
                lastikKm = Kalan(s["LASTIK_KM"]), lastikUyari = Isaret(s["LASTIKCHECK"]),
            });
        }));

        g.MapPut("/araclar/{id:int}", (int id, AracIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.AracYonetimi) return Yasak();
            if (string.IsNullOrWhiteSpace(i.Plaka)) return Hata("Plaka giriniz.");
            if (i.KadranKm is < 0 or > 10_000_000) return Hata("Geçersiz kadran km.");
            if (i.MotorKm is < 0 or > 1_000_000 || i.LastikKm is < 0 or > 1_000_000) return Hata("Geçersiz bakım km.");
            var p = P(k);
            p["id"] = id;
            p["plaka"] = Kes(i.Plaka, 150);
            p["aracSahibi"] = Kes(i.AracSahibi, 150);
            p["aracCinsi"] = Kes(i.AracCinsi, 64);
            p["aracMarkasi"] = Kes(i.AracMarkasi, 64);
            p["aracTipi"] = Kes(i.AracTipi, 64);
            p["surucu"] = Kes(i.Surucu, 150);
            p["aciklama"] = Kes(i.Aciklama, 150);
            p["ikon"] = i.Ikon is >= 1 and <= 99 ? i.Ikon : 1;
            p["m1"] = i.Muayene1; p["m2"] = i.Muayene2; p["s1"] = i.Sigorta1; p["s2"] = i.Sigorta2;
            p["k1"] = i.Kasko1; p["k2"] = i.Kasko2; p["e1"] = i.Egzoz1; p["e2"] = i.Egzoz2;
            p["kadranKm"] = i.KadranKm;
            p["motorKm"] = i.MotorKm; p["lastikKm"] = i.LastikKm;
            p["uM"] = B(i.MuayeneUyari); p["uS"] = B(i.SigortaUyari); p["uK"] = B(i.KaskoUyari); p["uE"] = B(i.EgzozUyari);
            p["uMotor"] = B(i.MotorUyari); p["uLastik"] = B(i.LastikUyari);
            var sonuc = await db.TekDegerAsync<int>(AracGuncelle, p, ct);
            return sonuc == 1 ? Results.Ok(new { mesaj = "Araç bilgileri güncellendi." }) : Results.NotFound(new { error = "Araç bulunamadı ya da size ait değil." });
        }));

        // ---- Grup (filo) yönetimi ----
        g.MapGet("/gruplar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.GrupYonetimi) return Yasak();
            return Results.Ok((await db.SorguAsync(GrupListesi, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                ad = Metin(s["GROUPNAME"]) ?? "",
                ustId = s["PARENTID"] is null || Convert.ToInt64(s["PARENTID"]) == 0 ? (int?)null : Convert.ToInt32(s["PARENTID"]),
                araclar = (s["UYELER"] as string ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray(),
            }));
        }));

        g.MapPost("/gruplar", (GrupIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Kullanici(h, o, ct, k => GrupYaz(null, i, k, db, ct)));
        g.MapPut("/gruplar/{id:int}", (int id, GrupIstegi i, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) =>
            Kullanici(h, o, ct, k => GrupYaz(id, i, k, db, ct)));

        g.MapDelete("/gruplar/{id:int}", (int id, HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
        {
            if (!k.GrupYonetimi) return Yasak();
            var p = P(k); p["id"] = id;
            return await db.CalistirAsync(GrupSil, p, ct) > 0
                ? Results.Ok(new { mesaj = "Filo bilgileri silindi." })
                : Results.NotFound(new { error = "Grup bulunamadı." });
        }));

        // ---- Arızalı / montajsız araçlar ----
        g.MapGet("/arizali-araclar", (HttpContext h, OturumServisi o, SqlDb db, CancellationToken ct) => Kullanici(h, o, ct, async k =>
            Results.Ok((await db.SorguAsync(ArizaliAraclar, P(k), ct)).Select(s => new
            {
                id = Convert.ToInt32(s["ID"]),
                cihazId = Metin(s["CIHAZID"]),
                plaka = Metin(s["PLAKA"]),
                sonSinyal = s["KONTROLTARIH"] as DateTime?,
                ilkSinyal = s["ILKSINYALTARIH"] as DateTime?,
                cihazTipi = Metin(s["CIHAZTIPI"]),
                aciklama = Metin(s["EHLIYETNO"]),
            }))));
    }

    private static async Task<IResult> GrupYaz(int? id, GrupIstegi i, OturumKullanici k, SqlDb db, CancellationToken ct)
    {
        if (!k.GrupYonetimi) return Yasak();
        if (string.IsNullOrWhiteSpace(i.Ad)) return Hata("Grup adı giriniz.");
        if (i.Ad.Trim().Length > 64) return Hata("Grup adı en fazla 64 karakter olabilir.");
        if (id is not null && i.UstId == id) return Hata("Grup kendi alt grubu olamaz.");
        var p = P(k);
        p["id"] = id;
        p["ad"] = i.Ad.Trim();
        p["ustId"] = i.UstId;
        p["uyeler"] = string.Join(',', (i.Araclar ?? []).Distinct());
        var sonuc = await db.TekDegerAsync<int>(GrupKaydet, p, ct);
        return sonuc switch
        {
            -1 => Hata("Bu grup adı mevcut, lütfen başka bir ad giriniz."),
            0 => Results.NotFound(new { error = "Grup bulunamadı." }),
            _ => Results.Ok(new { id = sonuc, mesaj = id is null ? "Yeni filo bilgileri kaydedildi." : "Filo bilgileri güncellendi." }),
        };
    }

    // ---- Yardımcılar ----
    private static async Task<IResult> Kullanici(HttpContext h, OturumServisi o, CancellationToken ct, Func<OturumKullanici, Task<IResult>> is_)
    {
        var k = await o.KullaniciAlAsync(h.User, ct);
        return k is null ? Results.Unauthorized() : await is_(k);
    }

    private static Dictionary<string, object?> P(OturumKullanici k) => new() { ["kullaniciId"] = k.Id };
    private static IResult Hata(string m) => Results.BadRequest(new { error = m });
    private static IResult Yasak() => Results.Json(new { error = "Bu işlem için yetkiniz yok." }, statusCode: StatusCodes.Status403Forbidden);
    private static string? Metin(object? v) => RaporYardimci.Metin(v);
    private static double? Ondalik(object? v) => RaporYardimci.Ondalik(v);
    private static string Kes(string? s, int uz) { var t = (s ?? "").Trim(); return t.Length > uz ? t[..uz] : t; }
    private static string B(bool b) => b ? "1" : "0";
    private static bool Isaret(object? v) => (v?.ToString() ?? "").Trim() == "1";
    private static string Saat(object? v) => (v?.ToString() ?? "").Trim() is { Length: > 0 } s && s != "-" ? (s.Contains(':') ? s : s.PadLeft(2, '0') + ":00") : "--:--";
    private static string? Gun(object? v) => v is DateTime d && d.Year > 1900 ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
}
