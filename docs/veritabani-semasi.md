# Karmobil veritabanı şeması (requests, SQL Server 2012)

Salt okunur sorguyla çıkarıldı: 2026-11-02 17:47. Tablo: 122, view: 9, prosedür/fonksiyon: 169.

## Tablolar (satır sayısına göre)

| Tablo | Satır |
|---|---:|
| konumlar | 146251344 |
| logs | 4547298 |
| seferozet | 4170504 |
| notificationLog | 1541473 |
| canbus | 230211 |
| mesafe | 218014 |
| _admin_cihazdurumu | 107077 |
| _admin_cihazdurumu_current | 107077 |
| ios_players | 34243 |
| gp_geoObjList | 30488 |
| cihazkomutLog | 26643 |
| fms_aracmarka | 7268 |
| mailalarmLogs | 5625 |
| uyarilar | 1832 |
| servislog | 1784 |
| cihazkomut | 1274 |
| userclients | 800 |
| groupmembers | 512 |
| languages | 482 |
| clients | 360 |
| logincheck | 264 |
| kadran | 226 |
| rentlog | 112 |
| mobilalarm | 106 |
| _iller | 82 |
| cihazsensor | 64 |
| regions | 41 |
| layer_icons | 34 |
| users | 33 |
| cihazalarm | 29 |
| groups | 24 |
| firmalar | 24 |
| usergroups | 24 |
| raportanim | 21 |
| userpoints | 15 |
| gecis_noktalari | 10 |
| mailalarm | 10 |
| canbusinputs | 10 |
| icons | 8 |
| gecis_tanim | 8 |
| fms_yakit | 6 |
| layer_sizes | 5 |
| mesajlar | 5 |
| routes | 4 |
| fms_ceza | 3 |
| layers | 2 |
| fms_muayene | 2 |
| fms_sigorta | 1 |
| fms_digermasraf | 1 |
| bayiler | 1 |
| sensorler | 1 |
| surucurapor | 1 |
| usermobiles | 0 |
| useryakitkart | 0 |
| yakit_kart_turu | 0 |
| yakit_kartlari | 0 |
| serverstatus | 0 |
| service_logins | 0 |
| userfirma | 0 |
| servistalep | 0 |
| smsalarm | 0 |
| sozlesme | 0 |
| spatial_ref_sys | 0 |
| surucukimlik | 0 |
| sade_main | 0 |
| sade_temp | 0 |
| sendmail | 0 |
| sendsms | 0 |
| messagein | 0 |
| messageout | 0 |
| motorbloke | 0 |
| navmsg | 0 |
| navpoints | 0 |
| opetfleets | 0 |
| ozelkomutlar | 0 |
| poi_task_details | 0 |
| poi_tasks | 0 |
| poi_tasks_archive | 0 |
| private_tasks | 0 |
| pts_servispoi | 0 |
| rapor_firma | 0 |
| raporlar | 0 |
| cihaztipleri | 0 |
| cihazkomutkod | 0 |
| fms_kaza | 0 |
| fms_lastik | 0 |
| fms_mtv | 0 |
| datatemp | 0 |
| fingerprintlog | 0 |
| fingerprintUsers | 0 |
| _mapulkeler | 0 |
| _mapulkeler2 | 0 |
| _oyala | 0 |
| _yetkili_cihazdurumu | 0 |
| _yontemmusterisi | 0 |
| activitylogs | 0 |
| adreskonum | 0 |
| alarm_timer | 0 |
| ayarlar | 0 |
| _admin_dia_carikod | 0 |
| _admin_dia_stok | 0 |
| _aveamusterisi | 0 |
| _devtakip_ekle | 0 |
| _diaborc | 0 |
| _hatlar | 0 |
| fms_ogs | 0 |
| fms_periyodik | 0 |
| gateways | 0 |
| gdr | 0 |
| gpdeneme | 0 |
| gper | 0 |
| genel_icons | 0 |
| genel_layers | 0 |
| genel_points | 0 |
| geometry_columns | 0 |
| getsms | 0 |
| gp | 0 |
| groups2 | 0 |
| gsmaccuracy | 0 |
| gsmpositions | 0 |
| mailsettings | 0 |
| maps | 0 |

## View'lar
- admin_clients_view
- admin_firmalar_short_view
- admin_firmalar_view
- admin_users_view
- clients_view
- fms_toplammasraflar
- groupmembers_view
- groups_view
- userclients_view

## Prosedür / fonksiyonlar
- Abonemi (SQL_SCALAR_FUNCTION)
- AdSoyadBul (SQL_SCALAR_FUNCTION)
- aktifMi (SQL_SCALAR_FUNCTION)
- bgtest (SQL_SCALAR_FUNCTION)
- bolgedeMi (SQL_SCALAR_FUNCTION)
- bolgedeMi3 (SQL_SCALAR_FUNCTION)
- calismasaatihesapla (SQL_SCALAR_FUNCTION)
- checkPOI (SQL_SCALAR_FUNCTION)
- checkPOI_ (SQL_SCALAR_FUNCTION)
- Distance2LastGSM (SQL_SCALAR_FUNCTION)
- driverByDateRange (SQL_SCALAR_FUNCTION)
- filoBul (SQL_SCALAR_FUNCTION)
- Firmakodu2MT (SQL_SCALAR_FUNCTION)
- fn_HttpPOST (SQL_SCALAR_FUNCTION)
- fnCalcDistance (SQL_SCALAR_FUNCTION)
- fncBase64_Encode (SQL_SCALAR_FUNCTION)
- gectiMi (SQL_SCALAR_FUNCTION)
- geoInGeography (SQL_SCALAR_FUNCTION)
- getGsmTitle (SQL_SCALAR_FUNCTION)
- getImei (SQL_SCALAR_FUNCTION)
- gp_filoBul (SQL_SCALAR_FUNCTION)
- gp_filoIdBul (SQL_SCALAR_FUNCTION)
- gp_filoIds (SQL_SCALAR_FUNCTION)
- gp_isDateInRange (SQL_SCALAR_FUNCTION)
- gp_poiSpeedAvg (SQL_SCALAR_FUNCTION)
- gp_poiTaskDistance (SQL_SCALAR_FUNCTION)
- gp_poiVisitTime (SQL_SCALAR_FUNCTION)
- gp_poiWaitTime (SQL_SCALAR_FUNCTION)
- GunlukKM (SQL_SCALAR_FUNCTION)
- GunlukKM2 (SQL_SCALAR_FUNCTION)
- gunlukMesafe (SQL_SCALAR_FUNCTION)
- hareketSuresi (SQL_SCALAR_FUNCTION)
- isAlarmTime (SQL_SCALAR_FUNCTION)
- isInPolygon (SQL_SCALAR_FUNCTION)
- isInPolygon2 (SQL_SCALAR_FUNCTION)
- isInPolygon3 (SQL_SCALAR_FUNCTION)
- isUserClient (SQL_SCALAR_FUNCTION)
- kadranKm (SQL_SCALAR_FUNCTION)
- kadranKmGecmisIzleme (SQL_SCALAR_FUNCTION)
- lastDriver (SQL_SCALAR_FUNCTION)
- latinYap (SQL_SCALAR_FUNCTION)
- mesafehesapla (SQL_SCALAR_FUNCTION)
- mesafehesaplatest (SQL_SCALAR_FUNCTION)
- mesafetablo (SQL_SCALAR_FUNCTION)
- MinutesToDuration (SQL_SCALAR_FUNCTION)
- PlakaBul (SQL_SCALAR_FUNCTION)
- poiKontrol (SQL_SCALAR_FUNCTION)
- poiKontrol2 (SQL_SCALAR_FUNCTION)
- polygonById (SQL_SCALAR_FUNCTION)
- queryToJSON (SQL_SCALAR_FUNCTION)
- rolantiSuresi (SQL_SCALAR_FUNCTION)
- rotadaMi (SQL_SCALAR_FUNCTION)
- sokakBul (SQL_SCALAR_FUNCTION)
- surucuAdi (SQL_SCALAR_FUNCTION)
- surucuBul (SQL_SCALAR_FUNCTION)
- txt_diff (SQL_SCALAR_FUNCTION)
- ulkebul (SQL_SCALAR_FUNCTION)
- UserBul (SQL_SCALAR_FUNCTION)
- VNtoMT (SQL_SCALAR_FUNCTION)
- wsIhlalSay (SQL_SCALAR_FUNCTION)
- wsSpeedAvg (SQL_SCALAR_FUNCTION)
- wsSpeedMax (SQL_SCALAR_FUNCTION)
- xy2addressPW (SQL_SCALAR_FUNCTION)
- YesNo (SQL_SCALAR_FUNCTION)
- YetkiliAdminBul (SQL_SCALAR_FUNCTION)
- addMobileGPSData (SQL_STORED_PROCEDURE)
- addMobileGPSData2 (SQL_STORED_PROCEDURE)
- aylikMesafe (SQL_STORED_PROCEDURE)
- aylikMesafe2 (SQL_STORED_PROCEDURE)
- BaglantiCihazLog (SQL_STORED_PROCEDURE)
- checkAlarms (SQL_STORED_PROCEDURE)
- checkAlarmsMain (SQL_STORED_PROCEDURE)
- checkBloke (SQL_STORED_PROCEDURE)
- checkPTS (SQL_STORED_PROCEDURE)
- CleanBadEmailSms (SQL_STORED_PROCEDURE)
- dailyReportMain (SQL_STORED_PROCEDURE)
- fixErrors (SQL_STORED_PROCEDURE)
- fixIosTokens (SQL_STORED_PROCEDURE)
- fixMailSms (SQL_STORED_PROCEDURE)
- fixMesafeTablo (SQL_STORED_PROCEDURE)
- fixMesafeTablotamir (SQL_STORED_PROCEDURE)
- fixMesaiMesafeTablo (SQL_STORED_PROCEDURE)
- fixMesaiMesafeTablo2 (SQL_STORED_PROCEDURE)
- fixSade (SQL_STORED_PROCEDURE)
- fixSK (SQL_STORED_PROCEDURE)
- fmsYakitEkle (SQL_STORED_PROCEDURE)
- geocoding (SQL_STORED_PROCEDURE)
- getPaymentKeys (SQL_STORED_PROCEDURE)
- gp_allGroupClients (SQL_STORED_PROCEDURE)
- gp_allUserGroups (SQL_STORED_PROCEDURE)
- gp_getClientDetail (SQL_STORED_PROCEDURE)
- gp_getGroupClients (SQL_STORED_PROCEDURE)
- gp_infoBox (SQL_STORED_PROCEDURE)
- gp_jsonData (SQL_STORED_PROCEDURE)
- gp_jsonDataPast (SQL_STORED_PROCEDURE)
- gp_jsonDataTest (SQL_STORED_PROCEDURE)
- gp_lastLocationByTitle (SQL_STORED_PROCEDURE)
- gp_parkRaporu (SQL_STORED_PROCEDURE)
- gp_poiTaskCheckAlarms (SQL_STORED_PROCEDURE)
- gp_poiTasks (SQL_STORED_PROCEDURE)
- gp_privateTaskCheckAlarms (SQL_STORED_PROCEDURE)
- gp_privateTaskDetail (SQL_STORED_PROCEDURE)
- gp_processPoiTasks (SQL_STORED_PROCEDURE)
- gp_reportLastAlarms (SQL_STORED_PROCEDURE)
- gp_reportPoiTasks (SQL_STORED_PROCEDURE)
- gp_reportPoiTasks2 (SQL_STORED_PROCEDURE)
- gp_reportTaskPoints (SQL_STORED_PROCEDURE)
- gp_sonAlarmlar (SQL_STORED_PROCEDURE)
- gp_terminalsInZone (SQL_STORED_PROCEDURE)
- HTTP_Request (SQL_STORED_PROCEDURE)
- kartkapat (SQL_STORED_PROCEDURE)
- moveMobileLicense (SQL_STORED_PROCEDURE)
- MTS_CreateKonum (SQL_STORED_PROCEDURE)
- raporFirma (SQL_STORED_PROCEDURE)
- raporFirmaTest (SQL_STORED_PROCEDURE)
- raporKimlik (SQL_STORED_PROCEDURE)
- reportActivity (SQL_STORED_PROCEDURE)
- reportActivity2 (SQL_STORED_PROCEDURE)
- reportActivity5 (SQL_STORED_PROCEDURE)
- reportAlarm (SQL_STORED_PROCEDURE)
- reportBolgeGenel (SQL_STORED_PROCEDURE)
- reportCihazKomutLogs (SQL_STORED_PROCEDURE)
- reportKontak (SQL_STORED_PROCEDURE)
- reportLastDay (SQL_STORED_PROCEDURE)
- reportPast (SQL_STORED_PROCEDURE)
- reportPos (SQL_STORED_PROCEDURE)
- reportPos2 (SQL_STORED_PROCEDURE)
- reportPosAsc (SQL_STORED_PROCEDURE)
- reportPosAsc2 (SQL_STORED_PROCEDURE)
- reportPosAscP (SQL_STORED_PROCEDURE)
- reportPosDriver (SQL_STORED_PROCEDURE)
- reportPosMobile (SQL_STORED_PROCEDURE)
- reportPosNew (SQL_STORED_PROCEDURE)
- reportPosNewKadran (SQL_STORED_PROCEDURE)
- reportPosNewKadranNoPoi (SQL_STORED_PROCEDURE)
- reportRegion (SQL_STORED_PROCEDURE)
- reportRegion2 (SQL_STORED_PROCEDURE)
- reportRegionWs (SQL_STORED_PROCEDURE)
- reportSpeed (SQL_STORED_PROCEDURE)
- reportSpeed2 (SQL_STORED_PROCEDURE)
- reportTemp (SQL_STORED_PROCEDURE)
- reportUnvisitedPoi (SQL_STORED_PROCEDURE)
- reportYolcuAsc (SQL_STORED_PROCEDURE)
- sendAlarm (SQL_STORED_PROCEDURE)
- sendAlarmNotification (SQL_STORED_PROCEDURE)
- sendAppLink (SQL_STORED_PROCEDURE)
- SendNotification (SQL_STORED_PROCEDURE)
- sendSMSAlarm (SQL_STORED_PROCEDURE)
- sendToFcm (SQL_STORED_PROCEDURE)
- SKfromAktifUsers (SQL_STORED_PROCEDURE)
- sonKimlik (SQL_STORED_PROCEDURE)
- sp_sendOneSignal (SQL_STORED_PROCEDURE)
- spHTTPRequest (SQL_STORED_PROCEDURE)
- spHTTPRequestXML (SQL_STORED_PROCEDURE)
- Teltonika_CreateKonum (SQL_STORED_PROCEDURE)
- Teltonika_UpdateClientIP (SQL_STORED_PROCEDURE)
- trcRequest (SQL_STORED_PROCEDURE)
- uyandir (SQL_STORED_PROCEDURE)
- wsAlarmHistory (SQL_STORED_PROCEDURE)
- wsAuthentication (SQL_STORED_PROCEDURE)
- wsGetAllVehicles (SQL_STORED_PROCEDURE)
- wsGetLastLocations (SQL_STORED_PROCEDURE)
- wsHeatReport (SQL_STORED_PROCEDURE)
- wsMileageReport (SQL_STORED_PROCEDURE)
- wsTrackingHistory (SQL_STORED_PROCEDURE)
- wsWorkingHourReport (SQL_STORED_PROCEDURE)
- gp_getUserClients (SQL_TABLE_VALUED_FUNCTION)
- gp_getUserGroups (SQL_TABLE_VALUED_FUNCTION)
- parseJSON (SQL_TABLE_VALUED_FUNCTION)

## Kolonlar

### _admin_cihazdurumu
- ID: bigint
- TARIH: datetime
- AKTIFARACLAR: int
- HESABIDONDURULMUS: int
- VERIALINAMAYAN: int
- MONTAJYAPILMAMIS: int
- KULLANICIATANMAMIS: int
- APNSIZ: int
- APNLI: int

### _admin_cihazdurumu_current
- ID: bigint
- TARIH: datetime
- AKTIFARACLAR: int
- HESABIDONDURULMUS: int
- VERIALINAMAYAN: int
- MONTAJYAPILMAMIS: int
- KULLANICIATANMAMIS: int
- APNSIZ: int
- APNLI: int

### _admin_dia_carikod
- ID: bigint
- CARIKODU: nvarchar(50)
- UNVAN: nvarchar(250)
- VERGINO: nvarchar(50)

### _admin_dia_stok
- ID: bigint
- DURUMU: nvarchar(50) null
- CLIENTID: bigint null
- UNVAN: nvarchar(250) null
- VERGINO: nvarchar(50) null

### _aveamusterisi
- ID: int
- UNVAN: nvarchar(250) null
- CARI: nvarchar(50) null
- VD: nvarchar(200) null
- VN: nvarchar(50) null
- TC: nvarchar(50) null

### _devtakip_ekle
- ID: bigint
- IMEI: nvarchar(50) null
- HARDWARE: int null
- STATUS: int

### _diaborc
- ID: int
- CARIKOD: nvarchar(50) null
- UNVAN: nvarchar(250) null
- VERGINO: nvarchar(50) null
- BORC: int null

### _hatlar
- gsmno: nvarchar(50) null
- yasindurum: nvarchar(50) null
- status: nvarchar(50) null
- bilgi1: nvarchar(50) null
- bilgi2: nvarchar(50) null
- bilgi3: nvarchar(50) null
- yontemtarih: datetime null
- devtakiptarih: datetime null
- eskidevtarih: datetime null

### _iller
- ID: int
- SEHIR: nvarchar(255) null

### _mapulkeler
- ogr_fid: int
- ogr_geometry: geometry(-1) null
- objectid: numeric null
- id_0: numeric null
- iso: nvarchar(3) null
- name_0: nvarchar(75) null
- id_1: numeric null
- name_1: nvarchar(75) null
- id_2: numeric null
- name_2: nvarchar(75) null
- id_3: numeric null
- name_3: nvarchar(75) null
- id_4: numeric null
- name_4: nvarchar(100) null
- id_5: numeric null
- name_5: nvarchar(75) null
- type_5: nvarchar(25) null
- engtype_5: nvarchar(25) null
- shape_leng: numeric null
- shape_area: numeric null
- info: nvarchar(250) null

### _mapulkeler2
- ogr_fid: int
- ogr_geometry: geometry(-1) null
- objectid: numeric null
- id_0: numeric null
- iso: nvarchar(3) null
- name_0: nvarchar(75) null
- id_1: numeric null
- name_1: nvarchar(75) null
- id_2: numeric null
- name_2: nvarchar(75) null
- id_3: numeric null
- name_3: nvarchar(75) null
- id_4: numeric null
- name_4: nvarchar(100) null
- id_5: numeric null
- name_5: nvarchar(75) null
- type_5: nvarchar(25) null
- engtype_5: nvarchar(25) null
- shape_leng: numeric null
- shape_area: numeric null
- info: nvarchar(250) null

### _oyala
- id: bigint
- not: nvarchar(45)

### _yetkili_cihazdurumu
- ID: bigint
- YUID: int
- TARIH: datetime
- TUM: int
- ARIZALI: int
- MONTAJEKSIGI: int

### _yontemmusterisi
- ID: int
- UNVAN: nvarchar(250) null
- CARI: nvarchar(50) null
- VD: nvarchar(200) null
- VN: nvarchar(50) null
- TC: nvarchar(50) null

### activitylogs
- ID: bigint
- USERID: bigint
- EUSERID: bigint
- EFIRMAKODU: nvarchar(50)
- ECIHAZID: nvarchar(50)
- EVENTS: nvarchar(-1)
- TARIH: datetime

### admin_clients_view
- ID: int
- CIHAZID: nvarchar(64)
- PLAKA: nvarchar(150)
- GSM: nvarchar(24)
- GSM_TARIH: datetime
- GPS_TARIH: datetime
- KONTAK: int null
- SKKONUM: nvarchar(765) null
- CIHAZTIPI: nvarchar(45)
- IPNO: nvarchar(16)
- PORT: nvarchar(10)
- DVRIPNO: nvarchar(132)
- ACIKLAMA: nvarchar(245)

### admin_firmalar_short_view
- FIRMAKODU: nvarchar(45)
- UNVAN: nvarchar(255) null
- BORC: bigint
- SEHIR: nvarchar(45) null
- STATUS: varchar(5)
- YUID: bigint null

### admin_firmalar_view
- ID: bigint
- FIRMAKODU: nvarchar(45)
- UNVAN: nvarchar(255) null
- BORC: bigint
- STATUS: varchar(5)
- MUSTERI_TEMSILCISI: nvarchar(255) null
- YETKILI: nvarchar(150) null
- SEHIR: nvarchar(45) null
- TEL: nvarchar(45) null
- GSM: nvarchar(45)
- FAX: nvarchar(45) null
- EMAIL: nvarchar(255) null
- WEBADRES: nvarchar(255) null
- ACIKLAMA: nvarchar(255) null
- ADRES: nvarchar(255) null
- YUID: bigint null

### admin_users_view
- ID: int
- FIRMAKODU: nvarchar(45)
- USERNAME: nvarchar(150) null
- USERLEVEL: nvarchar(150) null
- ANA_KULLANICISI: nvarchar(255) null
- ALARM_EMAIL: nvarchar(10) null
- ALARM_GSM: nvarchar(10) null
- EMAIL1: nvarchar(250)
- EMAIL2: nvarchar(250)
- EMAIL3: nvarchar(250)
- GSM1: nvarchar(16)
- GSM2: nvarchar(16)
- GSM3: nvarchar(16)
- SMSKREDI: int
- AKTIF: nvarchar(10) null
- ADI_SOYADI: nvarchar(50) null
- TEMA: nvarchar(50)
- SONAKTIVITE: datetime

### adreskonum
- ID: nvarchar(25)
- X: float null
- Y: float null
- KONUM: nvarchar(765) null
- STATUS: tinyint

### alarm_timer
- ID: bigint
- CIHAZID: nvarchar(50) null
- KOMUT: nvarchar(50) null
- KONUMID: bigint null
- TARIH: datetime null
- STATUS: int null
- DATA1: nvarchar(255) null
- DATA2: bigint null

### ayarlar
- id: bigint
- type: nvarchar(45)
- variablename: nvarchar(45)
- variablevalue: nvarchar(45)
- status: bigint

### bayiler
- ID: numeric
- BID: nvarchar(50)
- BGIMAGE: nvarchar(50) null
- LOGO: nvarchar(50) null
- LOGOW: int null
- LOGOH: int null
- TITLE: nvarchar(200)
- MENUCSS: nvarchar(50) null
- STATUS: smallint
- FIRMAKODU: nvarchar(50) null
- DESTEKTEL: nvarchar(50) null
- DESTEKWEB: nvarchar(100) null

### canbus
- ID: bigint
- CIHAZID: nvarchar(45)
- INPUT: nvarchar(45)
- VALUE: float
- TARIH: datetime

### canbusinputs
- ID: bigint
- KOMUT: nvarchar(45)
- TITLE: nvarchar(45)

### cihazalarm
- ID: int
- CIHAZID: nvarchar(64)
- IPNO: nvarchar(16)
- KOMUT: nvarchar(64)
- ALARM: nvarchar(128)
- STR1: nvarchar(64)
- STR2: nvarchar(64)
- NUM1: float
- NUM2: float
- NUM3: float
- NUM4: float
- STATUS: int
- TARIH: datetime null
- STARTDATE: datetime null
- ENDDATE: datetime null

### cihazkomut
- ID: int
- CIHAZID: nvarchar(64)
- IPNO: nvarchar(16)
- KOMUT: nvarchar(64)
- STR2: nvarchar(64)
- NUM1: float
- NUM2: float
- NUM3: float
- NUM4: float
- STATUS: bigint
- TARIH: datetime
- STR1: nvarchar(200)
- STR3: nvarchar(50)
- STR4: nvarchar(50)
- UID: bigint

### cihazkomutkod
- ID: bigint
- KOMUT: nvarchar(50)
- ACIKLAMA: nvarchar(50)

### cihazkomutLog
- ID: int
- CIHAZID: nvarchar(64)
- IPNO: nvarchar(16)
- KOMUT: nvarchar(64)
- STR1: nvarchar(200)
- STR2: nvarchar(64)
- STR3: nvarchar(50)
- STR4: nvarchar(50)
- NUM1: float
- NUM2: float
- NUM3: float
- NUM4: float
- STATUS: bigint
- TARIH: datetime
- UID: bigint

### cihazsensor
- ID: int
- CIHAZID: nvarchar(64)
- IPNO: nvarchar(16)
- KOMUT: nvarchar(64)
- ALARM: nvarchar(128)
- STR1: nvarchar(64)
- STR2: nvarchar(64)
- NUM1: float
- NUM2: float
- NUM3: float
- NUM4: float
- STATUS: int
- TARIH: datetime

### cihaztipleri
- ID: bigint
- CIHAZTIPI: nvarchar(50)
- ACIKLAMA: nvarchar(150)

### clients
- ID: int
- CIHAZID: nvarchar(64)
- PLAKA: nvarchar(150)
- GSM: nvarchar(24)
- IMEINO: nvarchar(150)
- ARACSAHIBI: nvarchar(150)
- ARACCINSI: nvarchar(64)
- ARACMARKASI: nvarchar(64)
- ARACTIPI: nvarchar(64)
- SOFOR: nvarchar(150)
- EHLIYETNO: nvarchar(150)
- DURUM: int
- IPNO: nvarchar(16)
- HATA: nvarchar(128) null
- KONTAK: int null
- ROTABOLGEID: bigint
- ROTABOLGESTATUS: int
- ROTABOLGETYPE: nvarchar(10)
- KONTROLTARIH: datetime
- BASLANGICKM: float
- BAKIMKM: float
- MTARIH1: datetime
- MTARIH2: datetime
- STARIH1: datetime
- STARIH2: datetime
- KTARIH1: datetime
- KTARIH2: datetime
- ETARIH1: datetime
- ETARIH2: datetime
- KIMLIKID: nvarchar(16)
- KIMLIKYON: int
- DBHOST: nvarchar(64)
- ICON: bigint
- HATA_ALARM: tinyint
- SKTARIH: datetime
- SKID: bigint
- FIRSTMOVE: datetime
- LASTSTOP: datetime
- CURRENTSPEED: bigint
- MONTAJT: datetime
- CIHAZTIPI: nvarchar(45)
- LASTPOSID: bigint
- ISI: float
- KAYITTARIHI: datetime
- KAPI_IO: tinyint
- MOTOR: tinyint
- GPSHATA: nvarchar(45)
- PORT: nvarchar(10)
- ACIKLAMA: nvarchar(245)
- M1_1: nvarchar(6)
- M1_2: nvarchar(6)
- M2_1: nvarchar(6)
- M2_2: nvarchar(6)
- M3_1: nvarchar(6)
- M3_2: nvarchar(6)
- M4_1: nvarchar(6)
- M4_2: nvarchar(6)
- M5_1: nvarchar(6)
- M5_2: nvarchar(6)
- M6_1: nvarchar(6)
- M6_2: nvarchar(6)
- M7_1: nvarchar(6)
- M7_2: nvarchar(6)
- DVRIPNO: nvarchar(132)
- PORTCL: nvarchar(10)
- SERVERIP: nvarchar(16)
- SKX: float null
- SKY: float null
- SKKONUM: nvarchar(765) null
- SKYON: float null
- SKMESAFE: float null
- SKRAPORKODU: nvarchar(4) null
- SKALARM: nvarchar(128) null
- SKSITUATIONS: nvarchar(45) null
- SKSURUCU: bigint null
- PERNR: int null
- NAV: int null
- MAXHIZ: int null
- MINMESAFE: bigint null
- MAXMESAFE: bigint null
- NAVTYPE: nvarchar(45) null
- SKYAKITSEVIYE: float
- SKYAKITPULSE: float
- UYKUMODU: int null
- VCCOUNT: int null
- VCSET: int null
- VCALARM: int null
- KADRAN_KM: bigint null
- KADRAN_KMTARIH: datetime null
- SERVER1: datetime null
- SERVER2: datetime null
- MOTOR_KM: float null
- LASTIK_KM: float null
- STATUS: tinyint
- GIZLI: tinyint
- DURUMACIKLAMA: nvarchar(200)
- ILKSINYALTARIH: datetime
- FMS: tinyint
- PTS: tinyint null
- RUHSAT_VERILDIGIILILCE: nvarchar(50) null
- RUHSAT_PLAKA: nvarchar(50) null
- RUHSAT_ILKTESCILTARIH: nvarchar(50) null
- RUHSAT_TESCILSIRANO: nvarchar(50) null
- RUHSAT_TESCILTARIH: nvarchar(50) null
- RUHSAT_MARKA: nvarchar(50) null
- RUHSAT_TIPI: nvarchar(50) null
- RUHSAT_TICARIADI: nvarchar(50) null
- RUHSAT_MODELYILI: nvarchar(50) null
- RUHSAT_ARACSINIFI: nvarchar(50) null
- RUHSAT_CINSI: nvarchar(50) null
- RUHSAT_RENGI: nvarchar(50) null
- RUHSAT_MOTORNO: nvarchar(50) null
- RUHSAT_SASENO: nvarchar(50) null
- RUHSAT_NETAGIRLIK: nvarchar(10) null
- RUHSAT_AZAMIYUK: nvarchar(10) null
- RUHSAT_KATARAGIRLIK: nvarchar(10) null
- RUHSAT_RAMORKYUKLU: nvarchar(10) null
- RUHSAT_KOLTUKSAYISI: nvarchar(10) null
- RUHSAT_YOLCUSAYISI: nvarchar(10) null
- RUHSAT_SILINDIRHACMI: nvarchar(10) null
- RUHSAT_MOTORGUCU: nvarchar(10) null
- RUHSAT_YAKITCINSI: nvarchar(20) null
- RUHSAT_GUCAGIRLIGI: nvarchar(20) null
- RUHSAT_KULLANIMAMACI: nvarchar(50) null
- RUHSAT_TIPONAYNO: nvarchar(50) null
- RUHSAT_TCKIMLIK: nvarchar(20) null
- RUHSAT_SOYADIUNVAN: nvarchar(100) null
- RUHSAT_ADI: nvarchar(50) null
- RUHSAT_ADRESI: nvarchar(250) null
- RUHSAT_HACIZ: nvarchar(250) null
- RUHSAT_NOTERTARIH: nvarchar(20) null
- RUHSAT_NOTERNO: nvarchar(20) null
- RUHSAT_NOTERINADI: nvarchar(50) null
- RUHSAT_DIGER: nvarchar(50) null
- RUHSAT_ONAYLAYANSICIL: nvarchar(50) null
- RUHSAT_BELGESERI: nvarchar(50) null
- RENTSTATUS: int
- CONNTYPE: nvarchar(10) null
- CONNEND: nvarchar(5)
- HAT: nvarchar(10) null
- BBB_AKTIF: smallint null
- BBB_CIHAZID: int null
- BBB_ARACID: int null
- KBB_AKTIF: int null
- SKWORKINGHOUR: bigint
- DEVICE_TYPE: tinyint null
- SKBATTERY: int
- ISITCHARGING: tinyint
- AT10INVERSE: smallint
- GSMHIZMETI: nvarchar(50) null
- HATBASLANGIC: datetime null
- HATBITIS: datetime null

### clients_view
- ID: int
- CIHAZID: nvarchar(64)
- PLAKA: nvarchar(150)
- GSM: nvarchar(24)
- IMEINO: nvarchar(150)
- ARACSAHIBI: nvarchar(150)
- ARACCINSI: nvarchar(64)
- ARACMARKASI: nvarchar(64)
- ARACTIPI: nvarchar(64)
- SOFOR: nvarchar(150)
- EHLIYETNO: nvarchar(150)
- DURUM: int
- IPNO: nvarchar(16)
- HATA: nvarchar(128) null
- KONTAK: int null
- ROTABOLGEID: bigint
- KONTROLTARIH: datetime
- CIHAZTIPI: nvarchar(45)
- STATUS: tinyint
- ROTABOLGESTATUS: int
- ROTABOLGETYPE: nvarchar(10)

### datatemp
- id: bigint
- dcihazid: nvarchar(45)
- dtype: nvarchar(45)
- dcommand: nvarchar(45)
- dcontent: nvarchar(250)
- ddate: datetime
- dstatus: tinyint

### fingerprintlog
- ID: int
- CIHAZID: nvarchar(16)
- TARIH: datetime
- FINGERTEMPLATE: nvarchar(1200) null
- PERSONALID: bigint null

### fingerprintUsers
- ID: int
- SICILNO: nvarchar(50)
- TARIH: datetime
- FINGERTEMPLATE: nvarchar(1200) null
- ADISOYADI: nvarchar(100)
- ACIKLAMA: nvarchar(250)

### firmalar
- ID: bigint
- UNVAN: nvarchar(255) null
- ADRES: nvarchar(255) null
- SEHIR: nvarchar(45) null
- TEL: nvarchar(45) null
- FAX: nvarchar(45) null
- YETKILI: nvarchar(150) null
- EMAIL: nvarchar(255) null
- WEBADRES: nvarchar(255) null
- ACIKLAMA: nvarchar(255) null
- YUID: bigint null
- STATUS: tinyint
- FIRMAKODU: nvarchar(45)
- BORC: bigint
- BID: nvarchar(200)
- YAKITKALANBAKIYE: float
- GSM: nvarchar(45)
- VERGINO: nvarchar(50)
- CREATEDATE: datetime
- BORCKITLE: tinyint
- BORCDATE: date
- ANKET1: int
- ANKET2: int
- ANKET3: int
- ANKETDATE: date
- ANKETACIKLAMA: nvarchar(250)

### fms_aracmarka
- ID: int
- MARKA: nvarchar(64)
- MODEL: nvarchar(64)

### fms_ceza
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- TUTANAKNO: nvarchar(50) null
- CEZAMADDESI: nvarchar(50) null
- SOFOR: nvarchar(50) null
- ADRES: nvarchar(64) null
- FIYAT: float null
- CEZAKONUSU: nvarchar(100) null
- ACIKLAMA: nvarchar(500) null

### fms_digermasraf
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- TURU: nvarchar(10) null
- MASRAFNEDENI: nvarchar(100) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### fms_kaza
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- TUTANAKNO: nvarchar(50) null
- KAZANEDENI: nvarchar(50) null
- SOFOR: nvarchar(50) null
- ADRES: nvarchar(64) null
- FIYAT: float null
- HASAR: nvarchar(100) null
- ACIKLAMA: nvarchar(500) null

### fms_lastik
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime
- TARIH: date null
- KILOMETRE: bigint null
- DEGISIMYERI: nvarchar(64) null
- DEGISIMNEDENI: nvarchar(64) null
- HANGILASTIK: nvarchar(64) null
- ADET: int null
- MARKA: nvarchar(64) null
- CAP: nvarchar(50) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### fms_mtv
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- YIL: nvarchar(10) null
- TAKSIT: nvarchar(10) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### fms_muayene
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- KILOMETRE: float null
- YAPTIRAN: nvarchar(50) null
- YAPILDIGIYER: nvarchar(64) null
- ISLEM: nvarchar(100) null
- DURUM: nvarchar(20) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### fms_ogs
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- TURU: nvarchar(10) null
- ODEMEKANALI: nvarchar(50) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### fms_periyodik
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime null
- TARIH: date null
- KILOMETRE: float null
- YAPTIRAN: nvarchar(50) null
- YAPILDIGIYER: nvarchar(64) null
- ISLEM: nvarchar(100) null
- DURUM: nvarchar(20) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### fms_sigorta
- ID: int
- CIHAZID: nvarchar(64) null
- RECDATE: datetime
- TARIH: date null
- POLICENO: nvarchar(50) null
- SIGORTAFIRMASI: nvarchar(50) null
- TIPI: nvarchar(20) null
- SURESI: int null
- FIYAT: float
- TRAMERNO: nvarchar(50) null
- TELEFON: nvarchar(50) null
- ACENTA: nvarchar(50) null
- ACIKLAMA: nvarchar(500) null
- UYARI: tinyint

### fms_toplammasraflar
- RPTURU: varchar(9)
- CIHAZID: nvarchar(64) null
- TARIH: datetime null
- URUN: nvarchar(151) null
- FIYAT: float null
- ACIKLAMA: nvarchar(659) null
- RPTABLE: varchar(15)

### fms_yakit
- ID: int
- CIHAZID: nvarchar(64)
- RECDATE: datetime
- TARIH: datetime null
- COMPANY: int null
- KILOMETRE: float null
- ALAN: nvarchar(50) null
- TURU: nvarchar(50) null
- LITRE: float null
- ODEMESEKLI: nvarchar(50) null
- FIYAT: float null
- ACIKLAMA: nvarchar(500) null

### gateways
- ID: int
- DBHOST: nvarchar(64)

### gdr
- id: int
- giris: datetime null
- cikis: datetime null
- ilk_giris: datetime null
- son_cikis: datetime null
- sure: datetime null
- mesai_sure: datetime null
- mesai_sure_top: datetime null
- sure_top: datetime null

### gecis_noktalari
- ID: bigint
- BASLIK: nvarchar(250) null
- NOKTALAR: ntext(1073741823) null

### gecis_tanim
- ID: bigint
- AD: nvarchar(250) null
- GIRIS_NOKTASI: bigint null
- CIKIS_NOKTASI: bigint null

### genel_icons
- ID: bigint
- ICONNO: bigint
- ICONNAME: nvarchar(45)

### genel_layers
- ID: int
- LAYERNAME: nvarchar(64)
- ICONID: int
- ZOOM: int
- ICONSIZE: int
- ICONCOLOR: nvarchar(8)
- TEXTCOLOR: nvarchar(8)
- BAYIFIRMA: nvarchar(200)
- STATUS: tinyint

### genel_points
- ID: int
- LAYERID: int
- TITLE: nvarchar(100)
- TELEFON: nvarchar(50)
- ADRES: nvarchar(150)
- DESCRIPTION: nvarchar(255)
- X: float
- Y: float
- TARIH: datetime

### geometry_columns
- f_table_catalog: varchar(128)
- f_table_schema: varchar(128)
- f_table_name: varchar(256)
- f_geometry_column: varchar(256)
- coord_dimension: int
- srid: int
- geometry_type: varchar(30)

### getsms
- ID: int
- GSM: nvarchar(20)
- MESAJ: nvarchar(160)
- STATUS: nvarchar(2)
- TARIH: datetime

### gp
- ID: int
- CIHAZID: nvarchar(64)
- PLAKA: nvarchar(150)
- GSM: nvarchar(24)
- IMEINO: nvarchar(150)
- ARACSAHIBI: nvarchar(150)
- ARACCINSI: nvarchar(64)
- ARACMARKASI: nvarchar(64)
- ARACTIPI: nvarchar(64)
- SOFOR: nvarchar(150)
- EHLIYETNO: nvarchar(150)
- DURUM: int
- IPNO: nvarchar(16)
- HATA: nvarchar(128) null
- KONTAK: int null
- ROTABOLGEID: nvarchar(64)
- RMESAFE: int
- KOMUTTYPE: nvarchar(10)
- KONTROLTARIH: datetime
- BASLANGICKM: float
- BAKIMKM: float
- MTARIH1: datetime
- MTARIH2: datetime
- STARIH1: datetime
- STARIH2: datetime
- KTARIH1: datetime
- KTARIH2: datetime
- ETARIH1: datetime
- ETARIH2: datetime
- KIMLIKID: nvarchar(16)
- KIMLIKYON: int
- DBHOST: nvarchar(64)
- ICON: bigint
- HATA_ALARM: tinyint
- SKTARIH: datetime
- SKID: bigint
- FIRSTMOVE: datetime
- LASTSTOP: datetime
- CURRENTSPEED: bigint
- MONTAJT: datetime
- CIHAZTIPI: nvarchar(45)
- LASTPOSID: bigint
- ISI: float
- KAYITTARIHI: datetime
- KAPI_IO: tinyint
- MOTOR: tinyint
- GPSHATA: nvarchar(45)
- PORT: nvarchar(10)
- ACIKLAMA: nvarchar(245)
- M1_1: nvarchar(6)
- M1_2: nvarchar(6)
- M2_1: nvarchar(6)
- M2_2: nvarchar(6)
- M3_1: nvarchar(6)
- M3_2: nvarchar(6)
- M4_1: nvarchar(6)
- M4_2: nvarchar(6)
- M5_1: nvarchar(6)
- M5_2: nvarchar(6)
- M6_1: nvarchar(6)
- M6_2: nvarchar(6)
- M7_1: nvarchar(6)
- M7_2: nvarchar(6)
- DVRIPNO: nvarchar(32)
- PORTCL: nvarchar(10)
- SERVERIP: nvarchar(16)
- SKX: float null
- SKY: float null
- SKKONUM: nvarchar(765) null
- SKYON: float null
- SKMESAFE: float null
- SKRAPORKODU: nvarchar(2) null
- SKALARM: nvarchar(128) null
- SKSITUATIONS: nvarchar(45) null
- SKSURUCU: bigint null

### gp_geoObjList
- ID: bigint
- IL: nvarchar(70) null
- ILCE: nvarchar(70) null
- MAHALLE: nvarchar(100) null
- TIP: nvarchar(16) null
- GEO_NAME: nvarchar(100) null
- GEO_TYPE: nvarchar(50) null
- GEOM: geography(-1) null
- POINTS: nvarchar(-1) null

### gpdeneme
- CIHAZID: nvarchar(64) null
- CIHAZTIPI: nvarchar(45) null
- KAYITTARIHI: datetime null
- deneme: int

### gper
- ID: int
- CIHAZID: nvarchar(64)
- PLAKA: nvarchar(150)
- GSM: nvarchar(24)
- IMEINO: nvarchar(150)
- ARACSAHIBI: nvarchar(150)
- ARACCINSI: nvarchar(64)
- ARACMARKASI: nvarchar(64)
- ARACTIPI: nvarchar(64)
- SOFOR: nvarchar(150)
- EHLIYETNO: nvarchar(150)
- DURUM: int
- IPNO: nvarchar(16)
- HATA: nvarchar(128) null
- KONTAK: int null
- ROTABOLGEID: nvarchar(64)
- RMESAFE: int
- KOMUTTYPE: nvarchar(10)
- KONTROLTARIH: datetime
- BASLANGICKM: float
- BAKIMKM: float
- MTARIH1: datetime
- MTARIH2: datetime
- STARIH1: datetime
- STARIH2: datetime
- KTARIH1: datetime
- KTARIH2: datetime
- ETARIH1: datetime
- ETARIH2: datetime
- KIMLIKID: nvarchar(16)
- KIMLIKYON: int
- DBHOST: nvarchar(64)
- ICON: bigint
- HATA_ALARM: tinyint
- SKTARIH: datetime
- SKID: bigint
- FIRSTMOVE: datetime
- LASTSTOP: datetime
- CURRENTSPEED: bigint
- MONTAJT: datetime
- CIHAZTIPI: nvarchar(45)
- LASTPOSID: bigint
- ISI: float
- KAYITTARIHI: datetime
- KAPI_IO: tinyint
- MOTOR: tinyint
- GPSHATA: nvarchar(45)
- PORT: nvarchar(10)
- ACIKLAMA: nvarchar(245)
- M1_1: nvarchar(6)
- M1_2: nvarchar(6)
- M2_1: nvarchar(6)
- M2_2: nvarchar(6)
- M3_1: nvarchar(6)
- M3_2: nvarchar(6)
- M4_1: nvarchar(6)
- M4_2: nvarchar(6)
- M5_1: nvarchar(6)
- M5_2: nvarchar(6)
- M6_1: nvarchar(6)
- M6_2: nvarchar(6)
- M7_1: nvarchar(6)
- M7_2: nvarchar(6)
- DVRIPNO: nvarchar(16)
- PORTCL: nvarchar(10)
- SERVERIP: nvarchar(16)
- SKX: float null
- SKY: float null
- SKKONUM: nvarchar(765) null
- SKYON: float null
- SKMESAFE: float null
- SKRAPORKODU: nvarchar(2) null
- SKALARM: nvarchar(128) null
- SKSITUATIONS: nvarchar(45) null
- SKSURUCU: bigint null

### groupmembers
- id: int
- GROUPID: int null
- CLIENTID: int null

### groupmembers_view
- id: int
- GROUPID: int null
- CLIENTID: int null

### groups
- ID: int
- GROUPNAME: nvarchar(64)
- USERID: int
- PARENTID: bigint null

### groups_view
- ID: int
- GROUPNAME: nvarchar(64)
- USERID: int
- USERNAME: nvarchar(150) null

### groups2
- ID: int
- GROUPNAME: nvarchar(64)
- USERID: int

### gsmaccuracy
- CIHAZID: nvarchar(50)
- X: float
- Y: float
- ACCURACY: float
- TARIH: datetime

### gsmpositions
- ID: bigint
- ADDRESS: nvarchar(255) null
- ALTITUDE: float null
- COURSE: float null
- ACCURACY: float null
- LATITUDE: float null
- LONGITUDE: float null
- OTHER: nvarchar(255) null
- POWER: float null
- SPEED: float null
- TIME: datetime null
- VALID: bit null
- DEVICE_ID: bigint null
- IMEI: nvarchar(255) null
- STATUS: smallint
- SERVERDATE: datetime
- APP_DB: nvarchar(50) null

### icons
- ID: bigint
- ICONNO: bigint
- ICONNAME: nvarchar(45)

### ios_players
- ID: bigint
- playerID: nvarchar(500) null
- TARIH: datetime null
- IPADRESI: nvarchar(50) null

### kadran
- ID: int
- CIHAZID: nvarchar(50) null
- MESAFE: bigint null

### konumlar
- ID: bigint
- CIHAZID: nvarchar(64) null
- CLIENTID: nvarchar(150) null
- X: float null
- Y: float null
- TARIH: datetime
- HIZ: int null
- KONUM: nvarchar(765) null
- KONTAK: int
- IPADRESS: nvarchar(17) null
- YON: float
- MESAFE: float
- RAPORKODU: nvarchar(2)
- ALARM: nvarchar(128) null
- YAKITSEVIYE: float
- YAKITPULSE: float
- SITUATIONS: nvarchar(45)
- ISI: float
- TRANSNO: bigint
- STATUS: tinyint
- SURUCU: nvarchar(20) null

### languages
- id: bigint
- prefix: nvarchar(45)
- txt: nvarchar(254)
- lang: nvarchar(15)

### layer_icons
- ID: bigint
- ICON_ID: bigint null
- ICON_NAME: nvarchar(50) null
- ICON_IMAGE: nvarchar(128) null

### layer_sizes
- ID: bigint
- SIZE_ID: bigint null
- SIZE_NAME: nvarchar(50) null

### layers
- ID: int
- USERID: int
- LAYERNAME: nvarchar(64)
- ICONID: int
- ICONSIZE: int
- ICONCOLOR: nvarchar(8)
- TEXTCOLOR: nvarchar(8)
- SUBUSERS: int null
- BAYIFIRMA: nvarchar(50)

### logincheck
- ID: int
- USERNAME: nvarchar(150) null
- IPADDR: nvarchar(16)

### logs
- id: bigint
- USERID: bigint
- ACTION: nvarchar(255)
- DATE: datetime
- IPNO: nvarchar(50)

### mailalarm
- ID: bigint
- CIHAZID: nvarchar(64)
- ALARM: nvarchar(20)
- EMAIL: nvarchar(250)

### mailalarmLogs
- ID: bigint
- CIHAZID: nvarchar(64) null
- ALARMKOMUT: nvarchar(10) null
- ALARMSUBJECT: nvarchar(-1) null
- ALARMBODY: nvarchar(-1) null
- RECIPIENTS: nvarchar(250) null
- TARIH: datetime

### mailsettings
- id: bigint
- host: nvarchar(30)
- name: nvarchar(30)
- mail: nvarchar(30)
- password: nvarchar(20)
- port: int
- status: nvarchar(6)

### maps
- ID: int
- MAPNAME: nvarchar(100)
- X1: float
- Y1: float
- X2: float
- Y2: float

### mesafe
- ID: bigint
- TARIH: date null
- CIHAZID: nvarchar(50) null
- MINM: float null
- MAXM: float null
- MESAI: float
- MINMTARIH: datetime
- MAXMTARIH: datetime
- MAXHIZ: int
- HIZIHLALSAY: int
- ILKKONTAKAC: datetime null
- SONKONTAKKAPAT: datetime null

### mesajlar
- ID: bigint
- KIMDEN: bigint null
- KIME: bigint null
- BASLIK: nvarchar(-1) null
- MESAJ: nvarchar(-1) null
- TARIH: datetime null
- DURUM: int null

### messagein
- id: int
- sender: nvarchar(30) null
- receiver: nvarchar(30) null
- msg: nvarchar(160) null
- senttime: nvarchar(100) null
- receivedtime: nvarchar(100) null
- operator: nvarchar(100) null
- msgtype: nvarchar(160) null
- reference: nvarchar(100) null
- status: int

### messageout
- id: int
- sender: nvarchar(30) null
- receiver: nvarchar(30) null
- msg: nvarchar(320) null
- senttime: nvarchar(100) null
- receivedtime: nvarchar(100) null
- reference: nvarchar(100) null
- status: nvarchar(20) null
- msgtype: nvarchar(160) null
- operator: nvarchar(100) null

### mobilalarm
- ID: bigint
- CIHAZID: nvarchar(50) null
- USERID: bigint null
- ALARM: nvarchar(20) null

### motorbloke
- ID: int
- CLIENTID: bigint
- M1_1: nvarchar(6) null
- M1_2: nvarchar(6) null
- M2_1: nvarchar(6) null
- M2_2: nvarchar(6) null
- M3_1: nvarchar(6) null
- M3_2: nvarchar(6) null
- M4_1: nvarchar(6) null
- M4_2: nvarchar(6) null
- M5_1: nvarchar(6) null
- M5_2: nvarchar(6) null
- M6_1: nvarchar(6) null
- M6_2: nvarchar(6) null
- M7_1: nvarchar(6) null
- M7_2: nvarchar(6) null

### navmsg
- ID: bigint
- CIHAZID: nvarchar(50) null
- TARIH: datetime
- MESAJ: nvarchar(200) null
- MSGTYPE: nvarchar(10) null
- STATUS: int null

### navpoints
- ID: bigint
- USERID: bigint null
- LISTNAME: nvarchar(50) null
- LISTPOINTS: nvarchar(-1) null

### notificationLog
- ID: bigint null
- TOKEN: nvarchar(500) null
- TITLE: nvarchar(150) null
- MSG: nvarchar(250) null
- TARIH: datetime

### opetfleets
- ID: bigint
- USERID: bigint null
- FLEETCODE: bigint null
- FLEETNAME: nvarchar(255) null

### ozelkomutlar
- ID: bigint
- TITLE: nvarchar(100)
- DATA: nvarchar(100)

### poi_task_details
- ID: bigint
- TARIH: date null
- CIHAZID: nvarchar(64) null
- LAYER_ID: bigint null
- POI_ID: bigint null
- VISIT_TIME: datetime null
- WAIT_TIME: bigint null
- TOTAL_VISIT: bigint null

### poi_tasks
- ID: bigint
- USERID: bigint null
- CIHAZID: nvarchar(64) null
- LAYERID: bigint null
- SAAT1: nvarchar(5) null
- SAAT2: nvarchar(5) null
- LAST_ALARM: datetime null
- ALARM_STATUS: tinyint null
- TASK_CREATED: date null

### poi_tasks_archive
- ID: bigint
- USERID: bigint null
- CIHAZID: nvarchar(64) null
- LAYERID: bigint null
- SAAT1: nvarchar(5) null
- SAAT2: nvarchar(5) null
- TASK_CREATED: date null
- TASK_TERMINATED: date null

### private_tasks
- ID: bigint
- USERID: bigint null
- CIHAZID: nvarchar(64) null
- POI_ID: bigint null
- TASK_NAME: nvarchar(150) null
- TASK_LAT: float null
- TASK_LNG: float null
- TASK_START: datetime null
- TASK_END: datetime null
- DIAMETER: bigint null
- TASK_CREATED: date null
- POSTIME_FIRST: datetime null
- POSTIME_LAST: datetime null
- ALARM_STATUS: tinyint null
- LAST_ALARM: datetime null
- TASK_STATUS: tinyint null

### pts_servispoi
- ID: int
- CIHAZID: nvarchar(64)
- POIID: int
- CAP: int
- GSM: nvarchar(50)
- EPOSTA: nvarchar(100)
- NOTLAR: nvarchar(500)
- DAY1A: nvarchar(10)
- DAY1B: nvarchar(10)
- DAY2A: nvarchar(10)
- DAY2B: nvarchar(10)
- DAY3A: nvarchar(10)
- DAY3B: nvarchar(10)
- DAY4A: nvarchar(10)
- DAY4B: nvarchar(10)
- DAY5A: nvarchar(10)
- DAY5B: nvarchar(10)
- DAY6A: nvarchar(10)
- DAY6B: nvarchar(10)
- DAY7A: nvarchar(10)
- DAY7B: nvarchar(10)
- RECDATE: datetime
- STATUS: tinyint null

### rapor_firma
- ID: bigint
- CIHAZID: nvarchar(50) null
- PLAKA: nvarchar(50) null
- MESAFE: bigint null
- MAXHIZ: bigint null
- FIRMAKODU: nvarchar(50) null

### raporlar
- ID: int
- CIHAZID: nvarchar(64) null
- CLIENTID: nvarchar(150) null
- X: float null
- Y: float null
- TARIH: datetime null
- HIZ: int null
- KONUM: nvarchar(765) null
- KONTAK: int
- IPADRESS: nvarchar(17)
- ALARM: nvarchar(128)
- RAPORKODU: nvarchar(2)
- YON: float
- MESAFE: float
- METREX: float
- METREY: float
- SITUATIONS: nvarchar(45)

### raportanim
- ID: int
- RAPORADI: nvarchar(128)
- RAPORKODU: nvarchar(128)

### regions
- ID: bigint
- REGION_NAME: nvarchar(45)
- REGION_COLOR: nvarchar(45)
- NOKTALAR: ntext(1073741823)
- USERID: bigint

### rentlog
- ID: bigint
- TARIH: datetime null
- USERID: bigint null
- CIHAZID: nvarchar(50) null
- ADSOYAD: nvarchar(50) null
- VERGIDAIRESI: nvarchar(50) null
- VERGINO: nvarchar(50) null
- TEL: nvarchar(15) null
- GSM: nvarchar(15) null
- FAX: nvarchar(15) null
- ADRES: nvarchar(255) null
- ILCE: nvarchar(50) null
- IL: nvarchar(50) null
- BASLANGIC: datetime null
- BITIS: datetime null
- KM1: float null
- KM2: float null
- FIYAT: float null
- NOTLAR: nvarchar(-1) null
- DURUM: int null
- KARALISTE: int null

### routes
- ID: int
- USERID: int
- TITLE: nvarchar(150)
- RENK: nvarchar(20) null
- ROUTE: geography(-1) null

### sade_main
- ID: bigint
- SERVERID: bigint null
- SERVERNAME: nvarchar(150) null
- FIRMID: bigint null
- FIRMNAME: nvarchar(150) null
- FIRMCREATEDATE: datetime null
- USERNAME: nvarchar(50) null
- PASSWORD: nvarchar(50) null
- FIRSTNAME: nvarchar(50) null
- LASTNAME: nvarchar(50) null
- SERIAL: bigint null
- IMEI: nvarchar(32) null
- PHONE: nvarchar(32) null
- LICENSEPLATE: nvarchar(50) null
- LASTONLINE: datetime null
- DEVICECREATEDATE: datetime null
- DEVICEMODELID: int null
- DEVICEMODELNAME: nvarchar(50) null
- MARKETERID: int null
- MARKETERNAME: nvarchar(50) null
- FIRMTYPEID: int null
- FIRMTYPENAME: nvarchar(50) null

### sade_temp
- ID: bigint
- SERVERID: bigint null
- SERVERNAME: nvarchar(150) null
- FIRMID: bigint null
- FIRMNAME: nvarchar(150) null
- FIRMCREATEDATE: datetime null
- USERNAME: nvarchar(50) null
- PASSWORD: nvarchar(50) null
- FIRSTNAME: nvarchar(50) null
- LASTNAME: nvarchar(50) null
- SERIAL: bigint null
- IMEI: nvarchar(32) null
- PHONE: nvarchar(32) null
- LICENSEPLATE: nvarchar(50) null
- LASTONLINE: datetime null
- DEVICECREATEDATE: datetime null
- DEVICEMODELID: int null
- DEVICEMODELNAME: nvarchar(50) null
- MARKETERID: int null
- MARKETERNAME: nvarchar(50) null
- FIRMTYPEID: int null
- FIRMTYPENAME: nvarchar(50) null

### seferozet
- ID: bigint
- CIHAZID: nvarchar(50)
- SURUCUID: nvarchar(50) null
- TARIHBASLA: datetime null
- KONUMBASLA: nvarchar(500) null
- TARIHBITIS: datetime null
- KONUMBITIS: nvarchar(500) null
- HIZMAX: int null
- HIZORTALAMA: int null
- ROLANTISURE: int null
- IHLALSAY: int null
- MESAFE: int null
- SEFERPUAN: int null

### sendmail
- ID: int
- MAIL: nvarchar(100)
- TITLE: nvarchar(100)
- MESAJ: nvarchar(160)
- STATUS: nvarchar(2)
- TARIH: datetime

### sendsms
- ID: int
- GSM: nvarchar(20)
- MESAJ: nvarchar(160)
- STATUS: nvarchar(2)
- TARIH: datetime

### sensorler
- ID: int
- SENSORADI: nvarchar(128) null
- SENSORKODU: nvarchar(3) null

### serverstatus
- ID: bigint
- PROCESS: nvarchar(50)
- DATE: datetime

### service_logins
- ID: bigint
- USERID: bigint null
- COMPANY: int null
- USERNAME: nvarchar(32) null
- PASSWORD: nvarchar(32) null
- FLEETCODE: nvarchar(32) null
- TOKENKEY: nvarchar(64) null
- TOKENDATE: datetime null
- LASTUPDATE: datetime null

### servislog
- ID: bigint
- USERID: bigint
- CIHAZID: nvarchar(50) null
- TARIH: datetime null
- DATATYPE: nvarchar(50) null
- OLD_VALUE: nvarchar(50) null
- NEW_VALUE: nvarchar(50) null

### servistalep
- ID: int
- USERID: int null
- CIHAZID: nvarchar(50) null
- PLAKA: nvarchar(50) null
- MESAJ: nvarchar(-1) null
- TARIH: datetime null
- STATUS: int null
- CEVAP: nvarchar(-1) null

### smsalarm
- ID: bigint
- CIHAZID: nvarchar(64)
- ALARM: nvarchar(20)
- GSM: nvarchar(20)

### sozlesme
- id: int
- userid: int null
- adsoyad: nvarchar(50) null
- tcno: nvarchar(50) null
- firma: nvarchar(100) null
- telefon: nvarchar(16) null
- eposta: nvarchar(50) null
- vdaire: nvarchar(50) null
- vno: nvarchar(24) null
- ipaddr: nvarchar(24) null
- tarih: datetime null

### spatial_ref_sys
- srid: int
- auth_name: varchar(256) null
- auth_srid: int null
- srtext: varchar(2048) null
- proj4text: varchar(2048) null

### surucukimlik
- ID: int
- KIMLIKID: nvarchar(16)
- ADSOYAD: nvarchar(128)
- EHLIYETNO: nvarchar(128)
- GSMNO: nvarchar(128)
- USERID: int
- CREATEDATE: datetime

### surucurapor
- ID: int
- CIHAZID: nvarchar(16)
- TARIH: datetime
- KIMLIKID: nvarchar(16)
- KIMLIKYON: int

### userclients
- ID: int
- USERID: int null
- CLIENTID: int null

### userclients_view
- ID: int
- USERID: int null
- CLIENTID: int null

### userfirma
- id: bigint
- USERID: bigint null
- FIRMA: nvarchar(100) null
- ADSOYAD: nvarchar(64) null
- ADRES: nvarchar(150) null
- TEL: nvarchar(16) null
- GSM: nvarchar(16) null
- EMAIL: nvarchar(64) null

### usergroups
- ID: bigint
- USERID: bigint null
- GROUPID: bigint null

### usermobiles
- ID: bigint
- USERID: bigint null
- CLIENTID: bigint null
- GSMNO: nvarchar(20) null
- TITLE: nvarchar(50) null
- SERVER1: datetime null
- SERVER2: datetime null
- LASTREQUEST: datetime null
- USER_IP: nvarchar(50) null
- CREATING_DATE: datetime null

### userpoints
- ID: int
- LAYERID: int
- TITLE: nvarchar(100)
- DESCRIPTION: nvarchar(255)
- X: float
- Y: float
- DIAMETER: bigint null
- TARIH: datetime
- POINT: geography(-1) null

### users
- ID: int
- USERNAME: nvarchar(150) null
- PASSWORD: nvarchar(150) null
- USERLEVEL: nvarchar(150) null
- SESSION_ID: nvarchar(64)
- CLIENTSESSION: nvarchar(64) null
- YC: int null
- YW: int null
- YG: int null
- YA: int null
- YP: int null
- YR: int null
- YMLT: bigint
- YY: int null
- YEXTRA: nvarchar(16)
- USERLIMIT: int
- GROUPID: int
- EMAIL1: nvarchar(250)
- EMAIL2: nvarchar(250)
- EMAIL3: nvarchar(250)
- EMAIL: nvarchar(2)
- GSM: nvarchar(2)
- GSM1: nvarchar(16)
- GSM2: nvarchar(16)
- GSM3: nvarchar(16)
- SMSKREDI: int
- BMAIL: nvarchar(9)
- DEMO: nvarchar(2)
- YH: int
- MAPX1: float
- MAPY1: float
- MAPX2: float
- MAPY2: float
- YACT: bigint
- SUPERVISOR: bigint
- FIRMAKODU: nvarchar(45)
- SOUND: bigint
- GORUNUM: bigint
- ACTIONDATE: datetime
- CREATEDATE: datetime
- HARITA: int null
- ADI_SOYADI: nvarchar(50) null
- BID: int null
- TEMA: nvarchar(50)
- ADMINRIGHTS: nvarchar(100)
- EPOSTAONAY: int null
- ARACLARIGRUPLA: smallint
- KOPUKLARIFILTRE: smallint
- WEBSERVIS: smallint
- AKILLIBLOKE: smallint
- CHECKBOX_STATES: nvarchar(16) null
- MOBILTOKEN: nvarchar(250)
- MOBILBILDIRIM: tinyint
- BORC: float null
- BORCDATE: datetime null

### useryakitkart
- ID: bigint
- USERID: bigint
- KARTID: varchar(50)
- TYPE: varchar(10)
- TARIH: datetime
- STATUS: smallint

### uyarilar
- ID: int
- USERID: int
- CIHAZID: nvarchar(64)
- MCHECK: nvarchar(2)
- SCHECK: nvarchar(2)
- KCHECK: nvarchar(2)
- ECHECK: nvarchar(2)
- MOTOR: nvarchar(2) null
- LASTIK: nvarchar(2) null

### yakit_kart_turu
- ID: bigint
- KARTTURU: nvarchar(100) null

### yakit_kartlari
- ID: bigint
- USERID: bigint null
- KARTTURU: nchar(32) null
- KARTNO: nchar(64) null
- BAKIYE: bigint null
- STATUS: int null
