# Karmobil yeni arayüz: sunucuya kurulum (Windows Server 2012 / IIS 8)

Geçici kurulum: geliştirme bitince yeni sunucuya taşınacak. Eski site (takip.karmobil.com)
olduğu gibi çalışmaya devam eder; yeni arayüz ayrı bir IIS sitesi olarak, ayrı alan adıyla
(ör. `yeni.karmobil.com`) yanında durur. İkisi aynı veritabanını kullanır.

## Paket
Geliştirme bilgisayarında:

```powershell
powershell -ExecutionPolicy Bypass -File C:\Projeler\KarmobilWeb\yayin\paketle.ps1
```

Çıktı: `C:\Projeler\KarmobilWeb\yayin\cikti\KarmobilWeb\`. Bu klasör sunucuya olduğu gibi kopyalanır.
Pakette parola ya da anahtar yoktur.

## 1. Sunucuya .NET kurulumu (bir kez)
1. https://dotnet.microsoft.com/download/dotnet/9.0 adresinde **ASP.NET Core Runtime** →
   **Windows → Hosting Bundle** dosyasını indirip kurun.
2. Kurulumdan sonra yönetici komut isteminde: `iisreset`
3. Kontrol: IIS Yöneticisi → sunucu → **Modules** listesinde `AspNetCoreModuleV2` görünmeli.

Kurulum hata verirse (2012'de eksik Windows güncellemesi olabilir) hata metnini iletin.

## 2. SQL kullanıcısı (bir kez)
`yayin\sql-kullanici.sql` dosyasını SQL Server Management Studio'da açın, `PAROLA` yerine
güçlü bir parola yazıp çalıştırın. Yeni API `sa` ile değil bu kısıtlı kullanıcıyla bağlanır.

## 3. Dosyalar
1. Paketi sunucuda örneğin `C:\inetpub\KarmobilWeb` klasörüne kopyalayın.
2. `appsettings.Production.ornek.json` dosyasının adını `appsettings.Production.json` yapın ve doldurun:
   - `ConnectionStrings:Karmobil`: SQL sunucu adresi + 2. adımdaki kullanıcı/parola.
   - `Auth:SecretKey`: rastgele anahtar. Sunucuda PowerShell ile üretin:
     ```powershell
     $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
     ```
   Bu dosya yalnız sunucuda durur; başka yere kopyalamayın.
   Doldurulmazsa uygulama bilerek açılmaz (geliştirme anahtarıyla çalışmasın diye).

## 4. IIS
1. **Application Pools → Add**: ad `KarmobilWeb`, **.NET CLR version: No Managed Code**, Integrated.
2. **Sites → Add Website**:
   - Site adı `KarmobilWeb`, uygulama havuzu `KarmobilWeb`, fiziksel yol `C:\inetpub\KarmobilWeb`
   - Binding: http, port 80, host name `yeni.karmobil.com`
3. Klasör izni: `C:\inetpub\KarmobilWeb` üzerinde `IIS AppPool\KarmobilWeb` için **Okuma**,
   `logs` alt klasörü için **Değiştirme**.

## 5. Alan adı ve HTTPS
1. DNS'te `yeni.karmobil.com` için A kaydı → sunucunun dış IP'si.
2. **HTTPS zorunlu**: giriş bilgileri ve oturum anahtarı açık HTTP'den geçmemeli.
   Eski sitenin sertifikası `*.karmobil.com` ise aynısı 443 bağlamasına seçilir; değilse
   win-acme (ücretsiz Let's Encrypt, IIS için) ile `yeni.karmobil.com` sertifikası alınır.
   Gerçek kullanıcılar HTTPS hazır olmadan kullanmamalı.

## 6. Kontrol
1. `https://yeni.karmobil.com/api/health` → `{"status":"ok"}`
2. `https://yeni.karmobil.com` → giriş ekranı; bir hesapla girip harita ve araçları kontrol edin.
3. Eski site ve mobil uygulama da çalışıyor olmalı (onlara dokunulmadı).

## Sorun giderme
| Hata | Sebep |
|---|---|
| HTTP 500.19 | Hosting Bundle kurulu değil ya da kurulumdan sonra `iisreset` yapılmadı |
| HTTP 500.30 / 500.31 | Uygulama açılamadı. Çoğunlukla `appsettings.Production.json` eksik/hatalı ya da SQL bağlantısı yok. Ayrıntı: Olay Görüntüleyicisi → Windows Logs → Application; ya da `web.config` içinde `stdoutLogEnabled="true"` yapıp `logs\` klasörüne bakın (sonra geri kapatın) |
| `/api/health` 503 | SQL sunucusuna erişilemiyor ya da kullanıcı/parola hatalı |

## Güncelleme (her yeni sürümde)
1. Geliştirme bilgisayarında `paketle.ps1` çalıştırın.
2. Sunucuda site klasörüne `app_offline.htm` adlı boş bir dosya koyun (uygulama durur, dosyalar serbest kalır).
3. Yeni paketi kopyalayın. **`appsettings.Production.json` dosyasının üzerine yazmayın** (pakette yoktur, yerinde kalır).
4. `app_offline.htm` dosyasını silin.
5. Yeni aşama yeni tablolara yazıyorsa `sql-kullanici.sql` güncellenmiş olur; güncel halini yeniden çalıştırın.

## Not
- .NET 9 desteği 10 Kasım 2026'da bitiyor, Windows Server 2012 de artık güncelleme almıyor.
  Bu kurulum geçicidir; yeni sunucuya taşırken .NET 10'a geçilecek.
