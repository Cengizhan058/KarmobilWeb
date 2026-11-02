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

    // data/conn3.asp: bayi kodu sabit "karmobil".
    public const string BayiBilgisi = """
        SELECT TOP 1 BID, TITLE, LOGO, FIRMAKODU, DESTEKTEL, DESTEKWEB
        FROM bayiler WITH (NOLOCK)
        WHERE STATUS = 1 AND BID = @bid
        """;
}
