using Karmobil.Api.Auth;
using Karmobil.Api.Data;
using Karmobil.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<SqlDb>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<OturumServisi>();

var authKey = AuthKey.Olustur(builder.Configuration);
builder.Services.AddSingleton(authKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // "sub" claim'i uzun ClaimTypes URI'sine eşlenmesin; OturumServisi "sub" ile arıyor.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = authKey.ValidationParameters;
    });
builder.Services.AddAuthorization();

// React geliştirme sunucusunun API'ye erişebilmesi için.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5180"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Content-Disposition"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Her isteğe bir correlationId atar; hata loglarında ve gövdesinde kullanılır.
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

// Beklenmeyen hatalarda SQL detayını dışarıya sızdırmadan JSON döner.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var feature = context.Features
        .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var correlationId = context.Items["CorrelationId"] as string;

    app.Logger.LogError(feature?.Error, "İstek işlenirken hata oluştu: {Path} [{CorrelationId}]", context.Request.Path, correlationId);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Sorgu çalıştırılırken bir hata oluştu.",
        code = "INTERNAL_ERROR",
        correlationId,
        detail = app.Environment.IsDevelopment() ? feature?.Error.Message : null
    });
}));

// Bağlantı sağlığı. Bu aşamada API veritabanına yazmaz: readOnly=true.
app.MapGet("/api/health", async (SqlDb db, CancellationToken ct) =>
{
    if (!db.Yapilandirilmis)
    {
        return Results.Json(new
        {
            status = "yapilandirilmamis",
            error = "Veritabanı bağlantısı girilmemiş (ConnectionStrings:Karmobil). Bkz. README.md.",
            readOnly = true
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var ok = await db.PingAsync(ct);
    return ok
        ? Results.Ok(new { status = "ok", target = db.Target, readOnly = true })
        : Results.Problem($"Veritabanına bağlanılamadı: {db.Target}", statusCode: 503);
})
.WithTags("Sistem");

app.MapAuth();
app.MapArac();

// ---------------------------------------------------------------------------
// Arayüzün API ile birlikte sunulması (isteğe bağlı)
//
// wwwroot altında derlenmiş arayüz varsa API onu da sunar; yoksa yalnızca API
// çalışır (geliştirmede npm run dev ile ayrı barındırılır).
// ---------------------------------------------------------------------------
var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var arayuzGomulu = File.Exists(Path.Combine(wwwroot, "index.html"));

if (arayuzGomulu)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(Path.Combine(wwwroot, "index.html"));
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SqlDb>();
    app.Logger.LogInformation(
        db.Yapilandirilmis
            ? $"Veritabanı hedefi: {db.Target} (salt okunur kullanım)"
            : "Veritabanı bağlantısı yapılandırılmamış (ConnectionStrings:Karmobil).");
}

app.Run();
