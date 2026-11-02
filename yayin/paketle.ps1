# Karmobil yayın paketi: arayüzü derler, API'yi yayınlar, arayüzü API'nin wwwroot'una koyar.
# Çıktı: yayin\cikti\KarmobilWeb  (IIS site klasörüne olduğu gibi kopyalanır)
# Bağlantı bilgisi ve anahtar pakete GİRMEZ; sunucuda appsettings.Production.json ile verilir.

$ErrorActionPreference = 'Stop'
$kok = Split-Path -Parent $PSScriptRoot
$cikti = Join-Path $PSScriptRoot 'cikti\KarmobilWeb'

Write-Host '1/3 Arayüz derleniyor...'
Push-Location (Join-Path $kok 'web')
try { npm run build; if ($LASTEXITCODE -ne 0) { throw 'Arayüz derlenemedi.' } } finally { Pop-Location }

Write-Host '2/3 API yayınlanıyor...'
if (Test-Path $cikti) { Remove-Item $cikti -Recurse -Force }
dotnet publish (Join-Path $kok 'api\Karmobil.Api.csproj') -c Release -o $cikti --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'API yayınlanamadı.' }

# Geliştirme ayar dosyaları pakete girmesin.
Get-ChildItem $cikti -Filter 'appsettings.*.json' | Remove-Item -Force

Write-Host '3/3 Arayüz wwwroot''a kopyalanıyor...'
Copy-Item (Join-Path $kok 'web\dist') (Join-Path $cikti 'wwwroot') -Recurse
Copy-Item (Join-Path $PSScriptRoot 'appsettings.Production.ornek.json') $cikti

New-Item -ItemType Directory -Force (Join-Path $cikti 'logs') | Out-Null
Write-Host "Hazır: $cikti"
