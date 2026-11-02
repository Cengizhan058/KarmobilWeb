-- Yeni API için kısıtlı SQL kullanıcısı (sa yerine).
-- SQL Server Management Studio'da sa ya da yönetici hesabıyla çalıştırın.
-- PAROLA yerine güçlü bir parola yazın; parolayı bu dosyaya kaydetmeyin.
--
-- Yetkiler: tüm tabloları okuma, saklı yordamları çalıştırma ve yalnız yeni arayüzün
-- yazdığı tablolara yazma. Yeni aşamalar yeni tablolara yazdıkça bu betiğe GRANT eklenir.

USE [master];
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'karmobil_web')
    CREATE LOGIN [karmobil_web] WITH PASSWORD = 'PAROLA', CHECK_POLICY = ON, DEFAULT_DATABASE = [requests];
GO

USE [requests];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'karmobil_web')
    CREATE USER [karmobil_web] FOR LOGIN [karmobil_web];
GO

ALTER ROLE [db_datareader] ADD MEMBER [karmobil_web];
GRANT EXECUTE TO [karmobil_web];

-- Görünüm tercihleri (yalnız bu üç kolon; SESSION_ID ve parola kolonlarına yazamaz)
GRANT UPDATE ON dbo.users (GORUNUM, ARACLARIGRUPLA, KOPUKLARIFILTRE) TO [karmobil_web];

-- Anlık konum isteği
GRANT INSERT, UPDATE ON dbo.cihazkomut TO [karmobil_web];

-- İşlemler: nokta grupları, noktalar, bölgeler, rotalar
GRANT INSERT, UPDATE, DELETE ON dbo.layers     TO [karmobil_web];
GRANT INSERT, UPDATE, DELETE ON dbo.userpoints TO [karmobil_web];
GRANT INSERT, UPDATE, DELETE ON dbo.regions    TO [karmobil_web];
GRANT INSERT, UPDATE, DELETE ON dbo.routes     TO [karmobil_web];
GO
