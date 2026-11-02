# Karmobil Web

takip.karmobil.com'un yeni arayüzü: **React 19 + Vite** (`web/`) ve **ASP.NET Core 9 API** (`api/`).
Eski Classic ASP site, mobil uygulama (KarmobilRN) ve cihaz gateway'i aynı SQL Server veritabanını
(`requests`) kullanmaya devam eder. Plan ve envanter: [docs/PLAN.md](docs/PLAN.md).

## Geliştirme

### API (http://localhost:5280)
Bağlantı bilgisi depoya girmez; bir kez user-secrets ile verilir:

```powershell
cd api
dotnet user-secrets set "ConnectionStrings:Karmobil" "Server=...;Database=requests;User Id=...;Password=...;Encrypt=False"
$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run
```

Kontrol: http://localhost:5280/api/health → `{"status":"ok", ...}`

### Arayüz (http://localhost:5180)
```powershell
cd web
npm install
npm run dev
```
`/api` istekleri Vite tarafından API'ye yönlendirilir (vite.config.ts).

## Kurallar
- Eski `data/*.asp` uçlarına, gateway'in yazdığı tablolara ve `users.SESSION_ID` gibi eski oturum alanlarına
  dokunulmaz. API şu an veritabanına hiç yazmaz (`/api/health` → `readOnly: true`).
- Kullanıcı girişi eski siteyle aynı `users` tablosundan (şifreler düz metin; hash'e geçiş mobil uygulamayla birlikte planlanacak).
- Tema seçimi `localStorage['km-theme']`: mavi (varsayılan), yesil, turuncu, bordo, mor, dark.
