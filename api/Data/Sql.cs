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
               MAPX1, MAPY1, MAPX2, MAPY2, TEMA
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
               c.SKKONUM AS ADRES, c.SKRAPORKODU AS RAPOR_KODU, c.SKALARM AS ALARM,
               c.DVRIPNO, c.MOTOR,
               dbo.kadranKm(c.CIHAZID) AS KADRAN_KM, dbo.gunlukMesafe(c.CIHAZID) AS GUNLUK_KM
        FROM clients c WITH (NOLOCK)
        WHERE {KullaniciAraclari}
          AND (@grupId IS NULL OR c.ID IN (SELECT CLIENTID FROM groupmembers WITH (NOLOCK) WHERE GROUPID = @grupId))
        ORDER BY c.PLAKA
        """;

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
