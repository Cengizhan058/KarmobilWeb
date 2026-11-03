# Karmobil Web - React + .NET API'ye taşıma planı

Tarih: 2026-11-02. Eski site: takip.karmobil.com (Classic ASP, dhtmlx 5.1, Leaflet).

## Kararlar (kullanıcı)
- Tüm ekranlar React'e taşınacak; eksik ekranla yayına alınmayacak.
- Sunucu tarafı: yeni **ASP.NET Core API** (.NET 9, HYS projesiyle aynı yapı: minimal API + JWT).
- Ön yüz: **React 19 + Vite + TypeScript + react-leaflet** (HYS ile aynı).
- Konum: `C:\Projeler\KarmobilWeb` + GitHub. Drive klasörüne kod/node_modules konmaz.
- Eski ASP sitesi, yeni temayla ara çözüm olarak yayına alınır (`C:\KarmobilTest\yukle`).
- Tema kararları React'te de geçerli: Roboto, ikon + yazı menü, renk temaları (mavi varsayılan, petrol, turuncu, bordo, mor, koyu).

## Mevcut durum (envanter)
- Veritabanı: SQL Server 2012 SP3, `requests`, sunucu 10.58.27.158. 122 tablo, 9 view, 169 prosedür/fonksiyon.
  Ayrıntı: [veritabani-semasi.md](veritabani-semasi.md).
- En büyük tablolar: `konumlar` ~146 milyon, `logs` ~4,5 milyon, `seferozet` ~4,2 milyon, `notificationLog` ~1,5 milyon.
- Eski site 156 `data/*.asp` ucu kullanıyor (88 XML, 62 HTML parçası). Liste: [eski-uclar.md](eski-uclar.md).
- Mobil uygulama (KarmobilRN) aynı ASP uçlarını kullanıyor → **eski uçlar ve tablolar bozulmamalı**.
- Araç cihazlarından gelen veriyi tablolara yazan ayrı bir **gateway** var (ayrıntısı bekleniyor).

## Veritabanı: değiştirelim mi?
**Karar (2026-11-02): PostgreSQL'e taşınacak, ama EN SONDA ayrı aşama olarak** (aşama 10).
Ön koşullar: yeni API tüm ekranları karşılamış olacak, mobil uygulama (KarmobilRN) eski ASP uçlarından yeni API'ye
geçmiş olacak, gateway (BiosXE5TCP / BiosUDP_XE5v26, Delphi) kaynak kodu alınıp PostgreSQL'e yazacak şekilde
güncellenecek. O zamana kadar yeni API MSSQL üzerinde geliştirilir; **tüm SQL `api/Data/Sql.cs` içinde tutulur**
(T-SQL'e özgü yapılar: `WITH (NOLOCK)`, `TOP`, `dbo.` fonksiyonları, `ISNULL`, `DATEDIFF` - taşımada buradan çevrilir).

Önceki değerlendirme (bilgi için):

- Gateway, mobil uygulama, eski site ve 169 prosedür aynı MSSQL'e bağlı. Motor değişirse (Oracle/PostgreSQL) gateway yeniden
  yazılır, 146 milyon konum satırı taşınır, prosedürler çevrilir. Arayüz yenilemesine katkısı yok, riski çok yüksek.
- Asıl sorun motor değil **sürüm**: SQL Server 2012'nin Microsoft desteği 2022'de bitti. Ayrı bir iş olarak
  SQL Server 2019/2022'ye yükseltme (aynı motor, uygulama değişikliği gerektirmez) önerilir.
- `konumlar` için arşivleme/bölümleme (partitioning) ayrıca değerlendirilir; geçmiş izleme raporlarının hızı buna bağlı.
- Yeni API **yalnızca okuma + kendi ekranlarının yazmaları** ile başlar; gateway'in yazdığı tablolara yapı değişikliği yapılmaz.
- Site eskiden `sa` kullanıcısıyla bağlanıyor. Yeni API için **ayrı, yetkisi sınırlı bir SQL kullanıcısı** açılmalı.

## Mimari
```
Cihazlar ──> Gateway ──> MSSQL (requests) <── eski ASP site / mobil uygulama (değişmez)
                              ^
                              └── Karmobil.Api (.NET 9, JWT) <── React web (yeni)
```
- Yayın: React derlemesi + API aynı IIS sunucusunda (ör. `takip.karmobil.com/yeni` ve `/yeni/api`), eski site paralel çalışır.
  Kullanıcılar hazır olunca varsayılan adres yeni siteye çevrilir.
- Kimlik: API, `USERS` tablosuyla doğrular (şifreler şu an düz metin; hash'e geçiş mobil uygulamayla birlikte planlanmalı).
  Yetkiler `USERS.YA/YG/YR/YP/YH/...` kolonlarından, araç erişimi `userclients` üzerinden.
- Canlı konum: ilk sürümde 5-10 sn'de bir sorgu; sonra gateway'den SignalR ile anlık yayın değerlendirilir.

## Aşamalar (hepsi bitmeden canlıya çıkmaz; geliştirme bu sırayla)
1. **İskelet**: repo, API (sağlık, giriş, JWT), React (giriş, tema, üst menü, boş harita).
2. **Harita çekirdeği**: araç listesi + durum sayaçları + filtre/grup, haritada araçlar, araç bilgi kartı, takip et.
3. **Geçmiş izleme**: tarih aralığı, rota çizimi, park noktaları, hız grafiği, Excel/PDF.
4. **Raporlar** (~20 rapor): son konumlar, günlük aktivite, aylık km, mesai, alarm/ihlal, kontak, sefer özeti, sürücü,
   havuz, park, rölanti, ısı, iş makinesi, bölge, POI, görevli, can-bus, esnek, bakım.
5. **İşlemler**: POI/nokta grupları, bölgeler, rotalar.
6. **Araçlar / Ayarlar**: araç ve grup yönetimi, ruhsat, kullanıcılar, RFID, mesai saatleri, bildirim ayarları,
   cihaz programlama, motor blokajı, aktif bildirimler.
7. **Gider yönetimi (FMS)**: sigorta, yakıt, muayene, bakım, lastik, ceza, kaza, MTV, OGS/HGS, genel masraf + raporlar.
8. **Diğer**: göstergeler (dashboard), mesajlar, DVR-kamera, destek, admin paneli (`admins/`).
9. **Yayın**: sunucu kurulumu, paralel çalıştırma, kullanıcı geçişi. Geçici kurulum mevcut Windows Server 2012 + IIS 8 üzerine .NET 9 ile (yayin/KURULUM.md, paketle.ps1, sql-kullanici.sql); geliştirme bitince yeni sunucuya + .NET 10 LTS.
10. **PostgreSQL'e taşıma**: mobil uygulamanın yeni API'ye geçişi, gateway güncellemesi, 169 prosedür/fonksiyonun
    PL/pgSQL'e çevrilmesi, `konumlar` (~146 milyon satır) tarih bölümlemeli taşınması, Sql.cs'in çevrilmesi.

## İlerleme
- [x] 1. İskelet (giriş, tema, menü, harita) - 9fbec00
- [x] 2. Araç listesi + canlı konum - db1e65b; araç bilgi paneli - cf64a96
- [x] 2. kalan: liste aç/kapa, kümeleme, Görünüm ayarları (etiket / grupla / arızalıları gizle; users tablosuna eski siteyle aynı alanlar), araç/kişi filtresi.
  Not: eski filtre ağacındaki 'Araç Durumu' (aktif/sabit nokta/arızalı/yetersiz personel) eski kodda hiç atanmayan
  cihaz_durum alanına bakıyordu, fiilen çalışmıyordu; taşınmadı.
- [x] 3. Geçmiş izleme: reportPosNewKadran(NoPoi) yordamı, hız renkli rota, duraklamalar (eşik seçilebilir), oynatma,
  liste (park gruplama), hız grafiği, Excel (CSV) ve Yazdır/PDF
- [x] 4. Raporlar: 21 rapor (genel IRapor altyapısı, /rapor/:kod sayfası). Demo hesabıyla hepsi hatasız çalıştı;
  ısı / can-bus / rfid / POI / sürücü puanı demo hesabında veri olmadığı için 0 satır - gerçek veriyle denenmeli.
  Açık: eski sitede DB'den dolan seçim listeleri (alarm türü, sürücü kimliği, nokta grubu/nokta, can-bus giriş türü,
  bölge) şimdilik metin alanı; bazı yordamların dönüş kolon adları ve konumsal parametreleri eski ASP'den çıkarıldı.
  İstisna: rapor SQL'leri Data/Sql.cs yerine her raporun kendi dosyasında (Raporlar/*.cs).
- [x] 5. İşlemler: /islemler ekranı (nokta grupları + POI, bölgeler, rotalar: ekle/düzenle/sil, haritaya tıklayarak çizim). Yazmalar yalnız kullanıcının kendi kayıtlarına; eski sitedeki iki sahiplik açığı (katman silerken başkasının noktaları, sahibi denetlenmeyen nokta ekleme) kapatıldı. Fark: eski rota ekranı yol takibi (routing) yapıyordu, yeni ekranda rota tıklanan noktalar arası düz çizgi; rota şekli eskisi gibi sonradan değiştirilemez (yalnız ad/renk).
- [x] Ek ekran (eski sitede yok): Araçlar → Cihaz Arıza Bildirimleri (/cihaz-sagligi). Son 7 günün konumlarından: enerji kesik / sökülmüş olabilir (E1 = enerji kesildi, E0 = geldi), sık enerji kesintisi, son 30 günde kesilen sinyal, kontak sinyali gelmiyor, km sayacı ilerlemiyor, sahte cihaz (F0), kadran km girilmemiş. İlk taramada 341 araçta 4 araç kontak+km, 1 araç enerji kesik çıktı.
- [ ] 6. Araçlar / Ayarlar: araç yönetimi (/arac-yonetimi), grup yönetimi (/grup-yonetimi), araç listesi (haritadaki listeyi açar), arızalı/montajsız (/arizali-araclar) yapıldı. Araç, ruhsat ve mesai kaydetme 2026-11-03'te test aracında (34SK9698) denendi, Türkçe karakter dahil doğru; değerler geri alındı. RF-ID kaydı denenmedi (demo hesabında kart yok); grup ekle/güncelle/sil demo hesabında denendi. Not: clients_update tetikleyicisi her güncellemede çalışır (eski sitede de aynı). Ruhsat (/ruhsat; eski uçta sahiplik denetimi yoktu, kapatıldı), mesai saatleri (/mesai-saatleri, seçilen araçlara toplu), RF-ID kartları (/rfid) yapıldı. Şifre değiştirme yapıldı (/sifre): eski _prcSetUserPwd.asp gibi mevcut şifre tutarsa users.PASSWORD güncellenir (önceki not yanlıştı, eski site de yazıyordu); en az 6 karakter. Geçici alt kullanıcıyla denendi. Salt okunur listeler yapıldı: aktif bildirimler, mevcut program tablosu, motor blokesi aktif araçlar (/liste/...), cihaz programlama geçmişi (rapor: cihazkomut_logs). Bildirim ayarları yapıldı (/bildirim-ayarlari): 10 alarm türü eski tablolara aynı biçimde (cihazalarm / cihazsensor / cihazkomut PH, bölge ihlalinde kullanıcı başına bölge satırları ve clients.ROTABOLGETYPE), alarm başına mailalarm / smsalarm; e-posta ve GSM alıcıları users.EMAIL1-3 / GSM1-3. Yalnız değiştirilen alarmlar gönderilir (hız alarmı cihaza komut olduğu için gereksiz yere tekrar gitmesin). Eski uçta sahiplik denetimi yoktu, kapatıldı; her kayıt tek işlemde. Fark: e-posta adresi değişince mailalarm satırları da yeni adrese taşınır (eskide bu kod yorum satırındaydı). Test aracında XD ve XI ile denendi (gateway XD'yi saniyeler içinde STATUS=1 yaptı), deneme satırları silindi. Kullanıcı yetkilendirme yapıldı (/alt-kullanicilar): alt kullanıcı ekle/güncelle/sil, yetkiler (harita, araç yönetimi, cihaz programlama, grup yönetimi, raporlama), görebileceği araçlar. Eski uçtaki açıklar kapatıldı: başkasının aracı atanabiliyordu, liste şifreleri tarayıcıya gönderiyordu. Farklar: yalnız ana kullanıcıda olan yetki verilebilir, alt kullanıcı sayısı USERLIMIT ile sınırlı, şifre en az 6 karakter. Şifreler düz metin kalır (eski site ve mobil giriş öyle doğruluyor). Demo hesabında geçici alt kullanıcıyla denendi (giriş, araç kısıtı, güncelleme), silindi. Cihaz programlama (/cihaz-programlama: cihazkomut PZ/PD/PM/PH, yalnız değiştirilenler, UID = kullanıcı; eski uyku modu KU hiç gönderilmiyordu, pil sıfırlama kullanılmıyor - alınmadı), akıllı motor blokaj (/akilli-blokaj: motorbloke haftalık plan, yetki AKILLIBLOKE; demo hesabında yetki yok, yalnız 403 denendi) ve bilgi panelinde motoru kilitle/aç düğmesi (yalnız clients.MOTOR 0/1 olan araçta; cihazkomut CIKIS4 + dbo.trcRequest; eski uçta sahiplik denetimi yoktu, araç hareketliyse uyarı) yapıldı. Test aracında PM aynı değerle yeniden gönderildi; 34SK9698'de blokaj donanımı yok (MOTOR=2), gerçek blokaj denenemedi. Eski not, yapıldı: kullanıcı yetkilendirme (alt kullanıcı hesapları), cihaz programlama ve akıllı motor blokaj (cihaza komut), şifre değiştirme.

- [x] 7. Gider yönetimi: 10 kayıt ekranı yapıldı (/gider/:tur - sigorta, yakıt, muayene, periyodik bakım, lastik, ceza, kaza, MTV, OGS/HGS, genel masraf). Alan ve seçenekler eski fms07.js ile aynı; menü yalnız gider takibi açık (clients.FMS = 1) aracı olana görünür (eskisi gibi). Eski uçlar yalnız ekliyordu, sahiplik denetimi yoktu ve SQL'e metin ekliyordu; yenisinde düzeltme/silme de var, kayıtlar kullanıcının FMS araçlarıyla sınırlı. Hepsi test aracında ekle/listele/güncelle/sil ile denendi. Sorun: fms_ceza ve fms_yakit'ın kimlik (identity) sayacı mevcut en büyük ID'nin gerisinde (ceza: yeni kayıt PK hatası verir; yakıtta PK yok, aynı ID'li kayıtlar var) - 2026-11-03'te kullanıcı onayıyla fms_ceza sayacı 3'e getirildi (DBCC CHECKIDENT RESEED), deneme kaydı ilk seferde eklendi; fms_yakit sayacı zaten MAX(ID)'nin önündeydi, dokunulmadı. fms_yakit'taki iki adet ID=2 kaydı duruyor. Raporlar yapıldı (genel rapor sayfası, /rapor/fms_masrafrapor, fms_custom, fms_toplamyakit): araç listesi yalnız gider takibi açık araçlar, rapor yetkisi yerine gider yetkisi aranır. Farklar: esnek rapordaki tutar süzgeci eskide sorguya hiç eklenmiyordu, artık her gider kaydına uygulanıyor; seçili türlerde gideri olmayan araç listelenmiyor. Genel yakıt raporunda km eskiden bir gün kayık hesaplanıyordu (başlangıç/bitişten önceki günün sayacı), artık aralığın ilk günü ilk sayaç - son günü son sayaç. Test aracında geçici kayıtlarla denendi, kayıtlar silindi.
- [ ] 8. Diğer: Göstergeler (/gostergeler; eski dashboard.asp: genel durum, mesai içi/dışı km, bugünkü bölge/hız/rölanti alarmları, en yüksek hız ve mesafe; eskide yalnız toplam/ortalama vardı, yenisinde ilk 5 araç da listelenir; eski sayfanın her açılışta cihazkomut 'SK' kayıtlarını STATUS=0 yapan yan etkisi alınmadı), Mesajlar (/mesajlar; okundu işareti ve silme yalnız kullanıcının kendi mesajlarında, menüde okunmamış rozeti), DVR-Kamera (/dvr; yalnız DVR adresi girilmiş araç varsa menüde; eski sayaç boş adresleri de sayıyordu, şu an hiçbir araçta gerçek adres yok), Destek (/destek; bayiler BID=karmobil, DESTEKTEL kayıtta '0212 000 00 00' görünüyor) yapıldı. Mesaj okundu/silme gerçek veriyle denenmedi (demo hesabında mesaj yok). Kalan: admin paneli (admins/).

## Açık sorular
- Gateway: nerede çalışıyor, hangi teknoloji, hangi tablolara yazıyor (`konumlar`, `clients`, `uyarilar`...)?
- KarmobilRN deposunun GitHub erişimi (API istemcisi ve tipler tekrar kullanılabilir).
