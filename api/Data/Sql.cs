namespace Karmobil.Api.Data;

/// <summary>
/// Tüm SQL metinleri. Tablo/kolon adları eski şemadan gelir (bkz. docs/veritabani-semasi.md);
/// davranış eski ASP dosyalarıyla aynı tutulur (login.asp, logincheck.asp, data/conn3.asp).
/// </summary>
public static class Sql
{
    // login.asp: USERNAME LIKE ... AND PASSWORD = ... (şifreler düz metin).
    // LIKE joker karakter açığı yaratmasın diye burada eşitlik kullanılır;
    // veritabanı harmanlaması büyük/küçük harf duyarsız olduğu için davranış aynıdır.
    public const string KullaniciGiris = """
        SELECT TOP 2 ID, USERNAME, PASSWORD, USERLEVEL, YACT,
               ISNULL(YA,0) AS YA, ISNULL(YW,0) AS YW, ISNULL(YG,0) AS YG,
               ISNULL(YR,0) AS YR, ISNULL(YP,0) AS YP, ISNULL(YH,0) AS YH
        FROM users WITH (NOLOCK)
        WHERE USERNAME = @kullaniciAdi AND PASSWORD = @sifre
        """;

    public const string OturumKullaniciBul = """
        SELECT ID, USERNAME, ADI_SOYADI, USERLEVEL, FIRMAKODU, YACT,
               ISNULL(YA,0) AS YA, ISNULL(YW,0) AS YW, ISNULL(YG,0) AS YG, ISNULL(YR,0) AS YR,
               ISNULL(YP,0) AS YP, ISNULL(YH,0) AS YH, ISNULL(YC,0) AS YC,
               ISNULL(SUPERVISOR,0) AS SUPERVISOR, ISNULL(USERLIMIT,0) AS USERLIMIT,
               ISNULL(AKILLIBLOKE,0) AS AKILLIBLOKE,
               CASE WHEN EXISTS (SELECT 1 FROM userclients uc WITH (NOLOCK) JOIN clients c WITH (NOLOCK) ON c.ID = uc.CLIENTID
                                 WHERE uc.USERID = users.ID AND c.STATUS = 1 AND c.FMS = 1) THEN 1 ELSE 0 END AS FMSVAR,
               CASE WHEN EXISTS (SELECT 1 FROM userclients uc WITH (NOLOCK) JOIN clients c WITH (NOLOCK) ON c.ID = uc.CLIENTID
                                 WHERE uc.USERID = users.ID AND c.STATUS = 1 AND ISNULL(LTRIM(RTRIM(c.DVRIPNO)), '') NOT IN ('', '1.1.1.1')) THEN 1 ELSE 0 END AS DVRVAR,
               MAPX1, MAPY1, MAPX2, MAPY2, TEMA,
               ISNULL(GORUNUM,0) AS GORUNUM, ISNULL(ARACLARIGRUPLA,0) AS ARACLARIGRUPLA, ISNULL(KOPUKLARIFILTRE,0) AS KOPUKLARIFILTRE
        FROM users WITH (NOLOCK)
        WHERE ID = @id
        """;

    // Kullanıcının erişebildiği araçlar: userclients + usermobiles (data/_jsondata.asp ile aynı küme).
    private const string KullaniciAraclari = """
        c.STATUS = 1 AND c.ID IN (
            SELECT CLIENTID FROM userclients WITH (NOLOCK) WHERE USERID = @kullaniciId
            UNION SELECT CLIENTID FROM usermobiles WITH (NOLOCK) WHERE USERID = @kullaniciId)
        """;

    // data/_jsondata.asp: canlı konum + durum. Durum kuralı aynı:
    // son sinyal (KONTROLTARIH) 24 dakikadan eskiyse "Veri Yok", hız < 5 ise kontağa göre Rölanti/Park.
    public const string AracKonumlari = $"""
        SELECT c.ID, c.CIHAZID, c.PLAKA, c.DEVICE_TYPE, UPPER(c.CIHAZTIPI) AS CIHAZTIPI,
               dbo.gp_filoIds(c.ID) AS FILO_IDS,
               CASE WHEN ABS(DATEDIFF(minute, c.KONTROLTARIH, GETDATE())) > 24 THEN 'VeriYok'
                    WHEN c.CURRENTSPEED < 5 THEN CASE WHEN c.KONTAK = '1' THEN 'Rolanti' ELSE 'Park' END
                    ELSE 'Hareketli' END AS DURUM,
               c.KONTAK, c.SKY AS LAT, c.SKX AS LON, c.CURRENTSPEED AS HIZ, c.SKYON AS YON,
               c.GPSHATA, c.SKBATTERY AS BATARYA, c.ISI,
               dbo.surucuBul(@kullaniciId, c.SOFOR) AS SURUCU,
               c.SKTARIH AS GPS_TARIH, c.KONTROLTARIH AS SINYAL_TARIH,
               DATEDIFF(hour, c.KONTROLTARIH, GETDATE()) AS SINYAL_YASI_SAAT,
               c.SKKONUM AS ADRES, c.SKRAPORKODU AS RAPOR_KODU, c.SKALARM AS ALARM,
               c.DVRIPNO, c.MOTOR,
               dbo.kadranKm(c.CIHAZID) AS KADRAN_KM, dbo.gunlukMesafe(c.CIHAZID) AS GUNLUK_KM
        FROM clients c WITH (NOLOCK)
        WHERE {KullaniciAraclari}
          AND (@grupId IS NULL OR c.ID IN (SELECT CLIENTID FROM groupmembers WITH (NOLOCK) WHERE GROUPID = @grupId))
        ORDER BY c.PLAKA
        """;

    // data/_getClientDetail.asp + _getClients.asp (Açıklama = EHLIYETNO, eski bilgi ekranıyla aynı).
    public const string AracDetay = $"""
        SELECT c.ID, c.CIHAZID, c.PLAKA, UPPER(c.CIHAZTIPI) AS CIHAZTIPI, c.CONNTYPE, c.SERVERIP, c.IPNO,
               c.GSM, c.IMEINO, c.ARACSAHIBI, c.ARACCINSI, c.ARACMARKASI, c.ARACTIPI, c.EHLIYETNO AS ACIKLAMA,
               c.DVRIPNO, c.MOTOR, c.SKBATTERY, c.KONTAK, c.LASTSTOP, c.SKKONUM, c.MAXHIZ,
               DATEDIFF(minute, c.LASTSTOP, GETDATE()) AS PARK_DAKIKA,
               c.ILKSINYALTARIH, c.KAYITTARIHI
        FROM clients c WITH (NOLOCK)
        WHERE c.ID = @aracId AND {KullaniciAraclari}
        """;

    // _aracDetay.asp: günlük mesafe (bugün 00:00'dan şimdiye).
    public const string GunlukMesafe = "SELECT dbo.mesafeHesapla(@cihazId, @ilk, @son) AS MESAFE";

    // _aracDetay.asp: son kontak açma (G2) son 24 saatte; kontak kapalıysa son duruştan önceki.
    public const string SonKontakAcma = """
        SELECT TOP 1 TARIH, KONUM FROM konumlar WITH (NOLOCK)
        WHERE CIHAZID = @cihazId AND RAPORKODU = 'G2'
          AND TARIH > DATEADD(hour, -24, GETDATE())
          AND (@sinir IS NULL OR TARIH <= @sinir)
        ORDER BY TARIH DESC
        """;

    // _aracDetay.asp: son alarm (son 24 saat).
    public const string SonAlarm = """
        SELECT TOP 1 TARIH, KONUM, ALARM FROM konumlar WITH (NOLOCK)
        WHERE CIHAZID = @cihazId AND RAPORKODU IN ('AA','AH','XD','XH','XR','BA','XE')
          AND TARIH > DATEADD(hour, -24, GETDATE())
        ORDER BY TARIH DESC
        """;

    // data/_prcAnlikKonum.asp ile aynı: gateway cihazkomut'taki SK kaydını görüp cihaza konum isteği yollar.
    // Cihaz IP'si 0.0.0.0 ya da HATA=9 ise komut yazılmaz (eski davranış).
    public const string AnlikKonumIste = """
        IF EXISTS (SELECT 1 FROM clients WITH (NOLOCK) WHERE STATUS = 1 AND CIHAZID = @cihazId AND IPNO <> '0.0.0.0' AND ISNULL(HATA,'') <> '9')
        BEGIN
            IF EXISTS (SELECT 1 FROM cihazkomut WHERE CIHAZID = @cihazId AND KOMUT = 'SK')
                UPDATE cihazkomut SET TARIH = GETDATE(), STATUS = 0 WHERE CIHAZID = @cihazId AND KOMUT = 'SK';
            ELSE
                INSERT INTO cihazkomut (CIHAZID, KOMUT, STR1, STR2, TARIH) VALUES (@cihazId, 'SK', '-', '-', GETDATE());
            SELECT 1;
        END
        ELSE SELECT 0;
        """;

    // Görünüm tercihleri: eski sitedeki _setClientView.asp (GORUNUM) ve _prcSetOption.asp
    // (ARACLARIGRUPLA, KOPUKLARIFILTRE) ile aynı alanlar; yalnızca oturumdaki kullanıcının kaydı.
    // NULL gelen parametre o alanı değiştirmez.
    public const string GorunumKaydet = """
        UPDATE users SET
            GORUNUM = COALESCE(@gorunum, GORUNUM),
            ARACLARIGRUPLA = COALESCE(@grupla, ARACLARIGRUPLA),
            KOPUKLARIFILTRE = COALESCE(@arizaliGizle, KOPUKLARIFILTRE)
        WHERE ID = @id
        """;

    // data/_raporKonum.asp: geçmiş konumlar saklı yordamla (POI hesaplı / hesapsız).
    // Tarihler yordamda nvarchar 'yyyy-MM-dd HH:mm:ss'.
    public const string GecmisKonumlar = "EXEC reportPosNewKadranNoPoi @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2, @user_id = @kullaniciId";
    public const string GecmisKonumlarPoi = "EXEC reportPosNewKadran @cihazid = @cihazId, @tarih1 = @t1, @tarih2 = @t2, @user_id = @kullaniciId";

    // data/_prcToplamMesafe.asp
    public const string AralikMesafe = "SELECT dbo.mesafeHesapla(@cihazId, @t1, @t2) AS MESAFE";

    // _raporKonum.asp: VI (kart okutma) kaydında SITUATIONS alanı sürücü kartıdır.
    public const string SurucuBul = "SELECT dbo.surucuBul(@kullaniciId, @kart) AS SURUCU";

    // data/_getFleets.asp
    public const string Gruplar = """
        SELECT ID, GROUPNAME, PARENTID FROM groups WITH (NOLOCK)
        WHERE USERID = @kullaniciId ORDER BY GROUPNAME
        """;

    public const string OkunmamisMesajSayisi = """
        SELECT COUNT(*) FROM mesajlar WITH (NOLOCK) WHERE KIME = @kullaniciId AND durum = '0'
        """;

    // data/conn3.asp: bayi kodu sabit "karmobil".
    public const string BayiBilgisi = """
        SELECT TOP 1 BID, TITLE, LOGO, FIRMAKODU, DESTEKTEL, DESTEKWEB
        FROM bayiler WITH (NOLOCK)
        WHERE STATUS = 1 AND BID = @bid
        """;
}
