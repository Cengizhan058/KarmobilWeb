# Eski sitenin kullandığı sunucu uçları (data/*.asp)

Ana ekran JS dosyalarından çıkarıldı. Yeni .NET API'nin karşılaması gereken işlev listesi. Mobil uygulama da bunların bir kısmını kullanır, **bu dosyalara dokunulmaz**.

| Uç | Dönen | Tablolar (SQL'de geçen) | Çağıran |
|---|---|---|---|
| _alarmPolygons | html | cihazalarm | main00aa36gp.js |
| _bolgeler | html | regions | main00aa36gp.js |
| _borcBakiyesi | - | firmalar | main00aa36gp.js |
| _firmaKontrol | html |  | main00aa36gp.js |
| _forms_fmsEsnek | xml | clients | fms07.js |
| _forms_raporAktivite2 | xml | clients, groups | reports005g9.js |
| _forms_raporAlarm | xml | clients, raportanim | reports005g9.js |
| _forms_raporBolge | xml | clients, regions | reports005g9.js |
| _forms_raporBolgeGenel | YOK |  | rent01.js |
| _forms_raporCanbus | xml | canbusinputs, clients | reports005g9.js |
| _forms_raporCihazKomut | xml | cihazkomutkod, clients | reports005g9.js |
| _forms_raporEsnek | xml | clients | reports005g9.js |
| _forms_raporHavuz | xml | clients, groups | reports005g9.js |
| _forms_raporHiz | xml | clients | reports005g9.js |
| _forms_raporIsi | xml | clients | reports005g9.js |
| _forms_raporIsiAylik | xml | clients | reports005g9.js |
| _forms_raporKimlik | xml | clients, surucukimlik | reports005g9.js |
| _forms_raporKontak | xml | clients | reports005g9.js |
| _forms_raporKontakTablo | xml | clients, groups | reports005g9.js |
| _forms_raporKonum | xml | clients | reports005g9.js |
| _forms_raporKonumMobile | xml | clients | mt00a09.js |
| _forms_raporMesafe | xml | clients | reports005g9.js |
| _forms_raporMesafeDetay | xml | clients | reports005g9.js |
| _forms_raporMesafeMesai | xml | clients, groups | reports005g9.js |
| _forms_raporMesafeTablo | xml | clients, groups | reports005g9.js |
| _forms_raporPark | xml | clients | reports005g9.js |
| _forms_raporParkMobile | xml | clients | reports005g9.js |
| _forms_raporRolanti | xml | clients | reports005g9.js |
| _forms_raporSurucuPuan | xml | clients, surucukimlik | reports005g9.js |
| _forms_raporTaksimetre | xml | cihazsensor, clients | reports005g9.js |
| _forms_raporToplamYakit | xml | clients | fms07.js |
| _forms_raporwm | xml | clients | reports005g9.js |
| _forms_raporYolcu | xml | clients | reports005g9.js |
| _getAlarmTable | xml | cihazalarm, clients, raportanim, regions, userclients | reports005g9.js |
| _getClientDetail | html | clients | main00aa36gp.js |
| _getClientIcons | html | icons | main00aa36gp.js |
| _getClients | html | clients, userclients, usermobiles | fms07.js, main00aa36gp.js |
| _getClientsAlarm | html | cihazalarm, cihazkomut, cihazsensor, clients, mailalarm, smsalarm | main00aa36gp.js |
| _getClientsBlokeliAraclar | xml | cihazkomut, clients | main00aa36gp.js |
| _getClientsPasifAraclar | xml | cihazkomut, clients | main00aa36gp.js |
| _getClientsPasifArizaAraclar | xml | clients | main00aa36gp.js |
| _getClientsProgram | html | cihazkomut, clients | main00aa36gp.js |
| _getClientsProgramTable | xml | cihazkomut, clients | main00aa36gp.js |
| _getFleets | html | groups | main00aa36gp.js |
| _getMobileClients | xml | usermobiles | mt00a09.js |
| _getMobileWorkHours | html | clients | mt00a09.js |
| _getProfile | html | sozlesme | main00aa36gp.js |
| _getPTSDetail | html | pts_servispoi | pts07.js |
| _getRfIdCards | html | surucukimlik | main00aa36gp.js |
| _getRFIDDetail | html | surucukimlik | main00aa36gp.js |
| _getSmartBlock | html | motorbloke | main00aa36gp.js |
| _getUserClients | html | clients, userclients, users | main00aa36gp.js |
| _getUsers | html | users | main00aa36gp.js |
| _getUserSettings | html | users | main00aa36gp.js |
| _getWorkHours | html | clients | main00aa36gp.js |
| _gridCeza | xml | fms_ceza | fms07.js |
| _gridDigerMasraf | xml | fms_digermasraf | fms07.js |
| _gridKaza | xml | fms_kaza | fms07.js |
| _gridLastik | xml | fms_lastik | fms07.js, pts07.js |
| _gridMTV | xml | fms_mtv | fms07.js |
| _gridMuayene | xml | fms_muayene | fms07.js |
| _gridOgs | xml | fms_ogs | fms07.js |
| _gridPeriyodik | xml | fms_periyodik | fms07.js |
| _gridPTSata | xml | userpoints | pts07.js |
| _gridRent | xml | rentlog | rent01.js |
| _gridSigorta | xml | fms_sigorta | fms07.js |
| _gridYakit | xml | fms_yakit | fms07.js |
| _invoices | xml | userfatura | main00aa36gp.js |
| _jsondata | html | clients, groupmembers, mesajlar, userclients, usermobiles | main00aa36gp.js |
| _layers | html | layers, userpoints | main00aa36gp.js |
| _menubarge | xml | clients, userclients | main00aa36gp.js |
| _messages | xml | mesajlar | main00aa36gp.js |
| _prcAlarm | html | cihazalarm, clients, mailalarm, regions, smsalarm, users | main00aa36gp.js |
| _prcBloke | html | cihazkomut | main00aa36gp.js |
| _prcBolge | html | regions | main00aa36gp.js |
| _prcCeza | html | fms_ceza | fms07.js |
| _prcClients | html | clients, kadran, userclients, uyarilar | main00aa36gp.js |
| _prcDigerMasraf | html | fms_digermasraf | fms07.js |
| _prcFirstMap | html | users | main00aa36gp.js |
| _prcFleets | html | groupmembers, groups | main00aa36gp.js |
| _prcKaza | html | fms_kaza | fms07.js |
| _prcKey | - |  | main00aa36gp.js, mt00a09.js |
| _prcLastik | html | fms_lastik | fms07.js, pts07.js |
| _prcLayer | html | layers, userpoints | main00aa36gp.js |
| _prcLayerPoints | html | layers, userpoints | main00aa36gp.js |
| _prcMessageDelete | html | mesajlar | main00aa36gp.js |
| _prcMessageReaded | html | mesajlar | main00aa36gp.js |
| _prcMobileRequest | html | usermobiles | mt00a09.js |
| _prcMobileWorkHours | html | clients, usermobiles | mt00a09.js |
| _prcMTV | html | fms_mtv | fms07.js |
| _prcMuayene | html | fms_muayene | fms07.js |
| _prcOgs | html | fms_ogs | fms07.js |
| _prcPeriyodik | html | fms_periyodik | fms07.js |
| _prcPilDurumu | html | clients | main00aa36gp.js |
| _prcProgram | html | cihazkomut, clients | main00aa36gp.js |
| _prcPTSata | html | pts_servispoi | pts07.js |
| _prcRent | html | clients, rentlog | rent01.js |
| _prcRFID | html | surucukimlik | main00aa36gp.js |
| _prcRoute | html | routes | main00aa36gp.js, reports005g9.js |
| _prcSetOption | - | users | main00aa36gp.js |
| _prcSetProfile | html | sozlesme | main00aa36gp.js |
| _prcSetUserPwd | html | users | main00aa36gp.js |
| _prcSigorta | html | fms_sigorta | fms07.js |
| _prcSmartBlock | html | clients, motorbloke, userclients | main00aa36gp.js |
| _prcSound | html | users | main00aa36gp.js |
| _prcSubscription | html | users | main00aa36gp.js |
| _prcToplamMesafe | html |  | reports005g9.js |
| _prcUsers | html | userclients, users | main00aa36gp.js |
| _prcUserSettings | html | clients, mailalarm, smsalarm, userclients, users | main00aa36gp.js |
| _prcWorkHours | html | clients, userclients | main00aa36gp.js |
| _prcYakit | html | fms_yakit | fms07.js |
| _raporAlarm | xml |  | reports005g9.js |
| _raporBakim | xml | clients | reports005g9.js |
| _raporBolgeGenel | YOK |  | rent01.js |
| _raporBolgeNew | xml | clients, regions, userclients | reports005g9.js |
| _raporCanbus | xml | canbus, clients | reports005g9.js |
| _raporCihazKomut | xml |  | reports005g9.js |
| _raporEsnek | xml | clients | reports005g9.js |
| _raporFMSEsnek | xml | clients, fms_toplammasraflar, userclients | fms07.js |
| _raporGunlukAktivite2 | xml | clients, groupmembers | reports005g9.js |
| _raporHavuz | xml | clients, groupmembers | reports005g9.js |
| _raporHiz | xml | clients | reports005g9.js |
| _raporIsi | xml |  | reports005g9.js |
| _raporIsiAylik | xml | clients, userclients | reports005g9.js |
| _raporKimlik | xml |  | reports005g9.js |
| _raporKontak | xml | clients, surucukimlik | reports005g9.js |
| _raporKontakTablo | xml | clients, groupmembers, mesafe | reports005g9.js |
| _raporKonum | xml |  | reports005g9.js |
| _raporKonumMobile | xml |  | mt00a09.js |
| _raporMasraflar | xml | fms_toplammasraflar | fms07.js |
| _raporMaxSpeed | xml | clients, mesafe, userclients | reports005g9.js |
| _raporMesafe | xml | cl | reports005g9.js |
| _raporMesafeDetay | xml | cl | reports005g9.js |
| _raporMesafeMesai | xml | clients, groupmembers, mesafe | reports005g9.js |
| _raporMesafeTablo | xml | clients, groupmembers, mesafe | reports005g9.js |
| _raporNokta | xml | clients, konumlar, layers, userpoints | reports005g9.js |
| _raporNoktaMobile | xml | clients, konumlar, layers, userpoints | mt00a09.js |
| _raporPark | xml | konumlar, layers, userpoints | reports005g9.js |
| _raporParkMobile | xml | clients, konumlar, layers, usermobiles, userpoints | reports005g9.js |
| _raporRolanti | xml | konumlar, layers, userpoints | reports005g9.js |
| _raporServer | xml | clients, userclients | reports005g9.js |
| _raporServer_hepsi | xml | clients, userclients | reports005g9.js |
| _raporSonAlarm | xml |  | main00aa36gp.js, reports005g9.js |
| _raporSonKonum | xml | clients, groupmembers | reports005g9.js |
| _raporSurucuPuan | xml | clients, seferozet, surucukimlik, userclients | reports005g9.js |
| _raporTaksimetre | xml | clients, konumlar, surucukimlik | reports005g9.js |
| _raporToplamYakit | xml | clients, fms_yakit, mesafe | fms07.js |
| _raporwm | xml | konumlar, layers, userpoints | reports005g9.js |
| _raporYolcu | xml | clients | reports005g9.js |
| _rotalar | html | routes | main00aa36gp.js |
| _ruhsatBilgileri | - | clients | fms07.js |
| _setClientView | html | users | main00aa36gp.js |
| _srvKontrol | html | clients, userclients | main00aa36gp.js |
| _topMenuRapor | xml |  | mt00a09.js, reports005g9.js |
| _uyari | xml | uyarilar | main00aa36gp.js |
| _uyariKontrol | html | uyarilar | main00aa36gp.js |
