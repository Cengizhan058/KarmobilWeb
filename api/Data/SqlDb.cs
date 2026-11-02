using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Karmobil.Api.Data;

/// <summary>
/// Tek veri erişim katmanı (SQL Server, "requests" veritabanı).
///
/// Bu veritabanını gateway (cihaz verisi), eski ASP site ve mobil uygulama da
/// kullanır. Yeni API kendi ekranlarının ihtiyacı dışında hiçbir tabloya
/// yazmaz; özellikle users.SESSION_ID gibi eski sitenin oturum alanlarına
/// dokunulmaz (eski sitedeki oturumları düşürür).
/// </summary>
public sealed class SqlDb(IConfiguration config, ILogger<SqlDb> logger)
{
    private readonly string _connectionString = config.GetConnectionString("Karmobil") ?? "";

    public bool Yapilandirilmis => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>Loglarda gösterilecek hedef (parola içermez).</summary>
    public string Target
    {
        get
        {
            if (!Yapilandirilmis) return "(yapılandırılmamış)";
            var b = new SqlConnectionStringBuilder(_connectionString);
            return $"{b.DataSource}/{b.InitialCatalog}";
        }
    }

    /// <summary>Açık bağlantı döndürür. Çağıran <c>await using</c> ile kapatmalıdır.</summary>
    public async Task<SqlConnection> AcAsync(CancellationToken ct)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    // -----------------------------------------------------------------------
    //  Okuma
    // -----------------------------------------------------------------------

    public async Task<List<Dictionary<string, object?>>> SorguAsync(
        string sql,
        IReadOnlyDictionary<string, object?>? parametreler = null,
        CancellationToken ct = default)
    {
        await using var conn = await AcAsync(ct);
        await using var cmd = KomutHazirla(conn, sql, parametreler);

        var satirlar = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var satir = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                satir[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);
            satirlar.Add(satir);
        }

        return satirlar;
    }

    public async Task<Dictionary<string, object?>?> TekSatirAsync(
        string sql,
        IReadOnlyDictionary<string, object?>? parametreler = null,
        CancellationToken ct = default)
    {
        var satirlar = await SorguAsync(sql, parametreler, ct);
        return satirlar.Count > 0 ? satirlar[0] : null;
    }

    public async Task<T?> TekDegerAsync<T>(
        string sql,
        IReadOnlyDictionary<string, object?>? parametreler = null,
        CancellationToken ct = default)
    {
        await using var conn = await AcAsync(ct);
        await using var cmd = KomutHazirla(conn, sql, parametreler);
        var sonuc = await cmd.ExecuteScalarAsync(ct);
        if (sonuc is null || sonuc == DBNull.Value) return default;
        if (sonuc is T dogrudan) return dogrudan;

        var hedef = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(sonuc, hedef, CultureInfo.InvariantCulture);
    }

    // -----------------------------------------------------------------------
    //  Yardımcılar
    // -----------------------------------------------------------------------

    private static SqlCommand KomutHazirla(
        SqlConnection conn,
        string sql,
        IReadOnlyDictionary<string, object?>? parametreler)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 60;

        if (parametreler is not null)
        {
            foreach (var (ad, deger) in parametreler)
                cmd.Parameters.AddWithValue("@" + ad, deger ?? DBNull.Value);
        }

        return cmd;
    }

    /// <summary>Bağlantı sağlık kontrolü.</summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await AcAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Karmobil veritabanı bağlantı kontrolü başarısız.");
            return false;
        }
    }
}
