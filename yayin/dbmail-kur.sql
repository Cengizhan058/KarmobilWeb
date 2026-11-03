/*
  Alarm mailleri için Database Mail kurulumu (sunucu: ARACTAKIP, SQL Server 2012).
  dbo.sendAlarm, msdb.dbo.sp_send_dbmail'i @profile_name = 'alarmats' ile çağırır; bu profil sunucuda yoktu
  ve "Database Mail XPs" kapalıydı (son alarm maili 2022-11-01).

  Çalıştırmadan önce aşağıdaki <...> değerlerini doldurun. Şifreyi bu dosyaya yazıp repoya göndermeyin.
  Tekrar çalıştırılabilir: hesap/profil varsa yeniden oluşturulmaz, yalnız ayarları güncellenir.
*/
DECLARE @smtpSunucu  sysname       = N'<smtp sunucusu, ör. mail.karmobil.com>';
DECLARE @port        int           = 587;            -- 587 (STARTTLS) ya da 465 / 25
DECLARE @ssl         bit           = 1;
DECLARE @kullanici   nvarchar(128) = N'<smtp kullanıcı adı>';
DECLARE @sifre       nvarchar(128) = N'<smtp şifresi>';
DECLARE @gonderen    nvarchar(128) = N'<gönderen adres, ör. alarm@karmobil.com>';
DECLARE @gorunenAd   nvarchar(128) = N'Karmobil Alarm';
DECLARE @testAdresi  nvarchar(256) = N'<deneme maili gidecek adres>';

-- 1) Database Mail'i aç
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'Database Mail XPs', 1; RECONFIGURE;

-- 2) Hesap
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.sysmail_account WHERE name = N'alarmats')
    EXEC msdb.dbo.sysmail_add_account_sp
        @account_name = N'alarmats', @email_address = @gonderen, @display_name = @gorunenAd, @replyto_address = @gonderen,
        @mailserver_name = @smtpSunucu, @port = @port, @enable_ssl = @ssl, @username = @kullanici, @password = @sifre;
ELSE
    EXEC msdb.dbo.sysmail_update_account_sp
        @account_name = N'alarmats', @email_address = @gonderen, @display_name = @gorunenAd, @replyto_address = @gonderen,
        @mailserver_name = @smtpSunucu, @mailserver_type = N'SMTP', @port = @port, @enable_ssl = @ssl, @username = @kullanici, @password = @sifre;

-- 3) Profil (sendAlarm'ın beklediği ad) ve hesabı profile bağla
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.sysmail_profile WHERE name = N'alarmats')
    EXEC msdb.dbo.sysmail_add_profile_sp @profile_name = N'alarmats', @description = N'Karmobil alarm mailleri (dbo.sendAlarm)';
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.sysmail_profileaccount pa
               JOIN msdb.dbo.sysmail_profile p ON p.profile_id = pa.profile_id
               JOIN msdb.dbo.sysmail_account a ON a.account_id = pa.account_id
               WHERE p.name = N'alarmats' AND a.name = N'alarmats')
    EXEC msdb.dbo.sysmail_add_profileaccount_sp @profile_name = N'alarmats', @account_name = N'alarmats', @sequence_number = 1;

-- 4) Deneme maili
EXEC msdb.dbo.sp_send_dbmail @profile_name = N'alarmats', @recipients = @testAdresi,
    @subject = N'Karmobil alarm maili denemesi', @body = N'<strong>Database Mail çalışıyor.</strong>', @body_format = N'HTML';

-- 5) Sonuç (birkaç saniye sonra tekrar çalıştırın): sent_status 'sent' olmalı; 'failed' ise hata günlüğüne bakın.
SELECT TOP 5 mailitem_id, recipients, subject, sent_status, send_request_date, sent_date FROM msdb.dbo.sysmail_allitems ORDER BY mailitem_id DESC;
SELECT TOP 5 log_date, event_type, description FROM msdb.dbo.sysmail_event_log ORDER BY log_id DESC;
