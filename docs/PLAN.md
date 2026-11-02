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

## Açık sorular
- Gateway: nerede çalışıyor, hangi teknoloji, hangi tablolara yazıyor (`konumlar`, `clients`, `uyarilar`...)?
- Canlı sunucunun Windows sürümü (.NET 9 Hosting Bundle uyumu) ve IIS erişimi.
- KarmobilRN deposunun GitHub erişimi (API istemcisi ve tipler tekrar kullanılabilir).
- Yeni GitHub deposunun adı/sahibi (ör. MhAliCakmakBelsoft/KarmobilWeb).
