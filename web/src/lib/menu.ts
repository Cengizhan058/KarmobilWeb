// Üst menü yapısı. Eski sitedeki data/_menubarge.asp ile aynı maddeler ve yetki koşulları.
// "yol" verilmeyen maddeler henüz taşınmadı; tıklanınca "yapım aşamasında" sayfası açılır.

import type { LucideIcon } from 'lucide-react'
import {
  Wrench, Car, Video, Settings, BarChart3, Mail, Eye, Wallet, Headset,
  Layers, MapPin, Pentagon, Route, Users, IdCard, List, TriangleAlert, KeyRound,
  ShieldCheck, BadgeCheck, Clock, Ban, Bell, Cpu, Table, Lock, History, CircleAlert,
  CalendarDays, Gauge, Flag, UserCheck, Warehouse, ParkingSquare, Hourglass, Thermometer,
  Tractor, SlidersHorizontal, Activity, Fuel, ClipboardCheck, Receipt, CarFront, FileText, Coins, Tag,
} from 'lucide-react'
import type { OturumKullanici } from '../api/client.ts'

export type MenuMaddesi = {
  id: string
  ad: string
  ikon: LucideIcon
  yol?: string
  /** Gezinme yerine görünüm ayarı (Görünüm menüsü): onay kutusu ya da etiket seçimi. */
  ayar?: 'grupla' | 'arizali' | 'etiket'
  goster?: (k: OturumKullanici) => boolean
  alt?: MenuMaddesi[]
}

const r = (k: OturumKullanici) => k.raporlar

export const MENU: MenuMaddesi[] = [
  {
    id: 'islemler', ad: 'İşlemler', ikon: Wrench, alt: [
      { id: 'layer_duzenle', ad: 'Nokta Grupları / Katmanlar', ikon: Layers, yol: '/islemler?sekme=nokta' },
      { id: 'poi_add', ad: 'Nokta / POI Ekle', ikon: MapPin, yol: '/islemler?sekme=nokta&yeni=1' },
      { id: 'poi_duzenle', ad: 'Nokta / POI Düzenle', ikon: MapPin, yol: '/islemler?sekme=nokta' },
      { id: 'bolge_ekle', ad: 'Yeni Bölge Oluştur', ikon: Pentagon, yol: '/islemler?sekme=bolge&yeni=1' },
      { id: 'bolge_duzenle', ad: 'Bölge Düzenle', ikon: Pentagon, yol: '/islemler?sekme=bolge' },
      { id: 'rota_duzenle', ad: 'Rotaları Düzenle / Sil', ikon: Route, yol: '/islemler?sekme=rota' },
    ],
  },
  {
    id: 'araclar', ad: 'Araçlar', ikon: Car, alt: [
      { id: 'ay', ad: 'Araç Yönetimi', ikon: CarFront, goster: k => k.aracYonetimi, yol: '/arac-yonetimi' },
      { id: 'fy', ad: 'Grup Yönetimi', ikon: Users, goster: k => k.grupYonetimi, yol: '/grup-yonetimi' },
      { id: 'fms_rb', ad: 'Ruhsat Bilgileri', ikon: IdCard, goster: k => k.aracYonetimi, yol: '/ruhsat' },
      { id: 'arac_listesi', ad: 'Araç Listesi', ikon: List, yol: '/?liste=1' },
      { id: 'pasifarizaaraclar', ad: 'Arızalı / Montajsız Araçlar', ikon: TriangleAlert, yol: '/arizali-araclar' },
      { id: 'cihaz_sagligi', ad: 'Cihaz Arıza Bildirimleri', ikon: Activity, yol: '/cihaz-sagligi' },
    ],
  },
  { id: 'dvrlist', ad: 'DVR-Kamera', ikon: Video },
  {
    id: 'ayarlar', ad: 'Ayarlar', ikon: Settings, alt: [
      { id: 'pwd_settings', ad: 'Şifre / Parola Ayarları', ikon: KeyRound, goster: k => k.kullaniciAyarlari },
      { id: 'sub_users', ad: 'Kullanıcı Yetkilendirme', ikon: ShieldCheck, goster: k => k.kullaniciAyarlari && k.altKullaniciLimiti > 0 },
      { id: 'rfid_management', ad: 'RF-ID Kart Yönetimi', ikon: BadgeCheck, yol: '/rfid' },
      { id: 'work_hours', ad: 'Mesai Saatleri', ikon: Clock, yol: '/mesai-saatleri' },
      { id: 'smart_block', ad: 'Akıllı Motor Blokaj', ikon: Ban, goster: k => k.akilliBlokaj },
      { id: 'alarm_settings', ad: 'Bildirim Ayarları', ikon: Bell },
      { id: 'device_settings', ad: 'Cihaz Programlama', ikon: Cpu, goster: k => k.cihazProgramlama },
      { id: 'program_table', ad: 'Mevcut Program Tablosu', ikon: Table, goster: k => k.cihazProgramlama, yol: '/liste/program-tablosu' },
      { id: 'blokeliaraclar', ad: 'Motor Blokesi Aktif Araçlar', ikon: Lock, goster: k => k.cihazProgramlama, yol: '/liste/blokeli-araclar' },
      { id: 'cihazkomut_logs', ad: 'Cihaz Programlama Geçmişi', ikon: History, goster: k => k.cihazProgramlama, yol: '/rapor/cihazkomut_logs' },
      { id: 'alarm_table', ad: 'Aktif Bildirimler', ikon: CircleAlert, yol: '/liste/aktif-bildirimler' },
    ],
  },
  {
    id: 'raporlar', ad: 'Raporlar', ikon: BarChart3, alt: [
      { id: 'report_last', ad: 'Son Konumlar', ikon: MapPin },
      { id: 'report_past', ad: 'Geçmiş İzleme', ikon: History, goster: r, yol: '/gecmis' },
      { id: 'report_activity', ad: 'Günlük Aktivite Raporu', ikon: CalendarDays, goster: r },
      { id: 'report_distance_tbl', ad: 'Aylık KM (Tablo) Raporu', ikon: Route, goster: r },
      { id: 'report_workhours', ad: 'Toplam Mesai KM Raporu', ikon: Clock, goster: r },
      { id: 'report_alarms', ad: 'Alarm / İhlal Raporu', ikon: TriangleAlert, goster: r },
      { id: 'report_kontak_tbl', ad: 'İlk / Son Kontak Tablosu', ikon: KeyRound, goster: r },
      { id: 'report_ignition', ad: 'Sefer Özeti Raporu', ikon: Flag, goster: r },
      { id: 'report_driverScore', ad: 'Sürücü Değerlendirme Raporu', ikon: UserCheck, goster: r },
      { id: 'report_havuz', ad: 'Havuz Verimlilik Raporu', ikon: Warehouse, goster: r },
      { id: 'report_park', ad: 'Park Raporu', ikon: ParkingSquare, goster: r },
      { id: 'report_idling', ad: 'Rölanti Raporu', ikon: Hourglass, goster: r },
      { id: 'report_temp', ad: 'Isı Raporu', ikon: Thermometer, goster: r },
      { id: 'report_temp_monthly', ad: 'Aylık Isı Aşım Raporu', ikon: Thermometer, goster: r },
      { id: 'report_wm', ad: 'İş Makinesi Çalışma Raporu', ikon: Tractor, goster: r },
      { id: 'report_region', ad: 'Bölge Giriş Çıkış Raporu', ikon: Pentagon, goster: r },
      { id: 'report_poi', ad: 'Nokta / POI Raporu', ikon: MapPin, goster: r },
      { id: 'report_rfid', ad: 'Görevli Raporu', ikon: BadgeCheck, goster: r },
      { id: 'report_canbus', ad: 'Can-Bus Raporu', ikon: Cpu, goster: r },
      { id: 'report_custom', ad: 'Esnek Rapor', ikon: SlidersHorizontal, goster: r },
      { id: 'report_bakim', ad: 'Araç Bakım Raporu', ikon: Wrench, goster: r },
    ],
  },
  { id: 'mesajlar', ad: 'Mesajlar', ikon: Mail },
  {
    id: 'gorunum', ad: 'Görünüm', ikon: Eye, alt: [
      { id: 'arac_grupla', ad: 'Araçları haritada grupla', ikon: Layers, ayar: 'grupla' },
      { id: 'arizali_gosterme', ad: 'Arızalı araçları gösterme (72 sa+ sinyalsiz)', ikon: TriangleAlert, ayar: 'arizali' },
      { id: 'client_view', ad: 'Araç etiketi', ikon: Tag, ayar: 'etiket' },
    ],
  },
  {
    id: 'gider', ad: 'Gider Yönetimi', ikon: Wallet, goster: k => k.giderYonetimi, alt: [
      { id: 'fms_masrafrapor', ad: 'Masraf Raporları', ikon: FileText },
      { id: 'fms_custom', ad: 'Esnek Rapor', ikon: SlidersHorizontal },
      { id: 'fms_toplamyakit', ad: 'Genel Yakıt Raporu', ikon: Fuel },
      { id: 'fms_sigorta', ad: 'Sigortalar', ikon: ShieldCheck, yol: '/gider/sigorta' },
      { id: 'fms_yakit', ad: 'Yakıt Alımları', ikon: Fuel, yol: '/gider/yakit' },
      { id: 'fms_muayene', ad: 'Muayene', ikon: ClipboardCheck, yol: '/gider/muayene' },
      { id: 'fms_periyodik', ad: 'Periyodik Bakım', ikon: Wrench, yol: '/gider/periyodik' },
      { id: 'fms_lastik', ad: 'Lastik Değişimi', ikon: Gauge, yol: '/gider/lastik' },
      { id: 'fms_ceza', ad: 'Trafik Cezası', ikon: Receipt, yol: '/gider/ceza' },
      { id: 'fms_kaza', ad: 'Trafik Kazası', ikon: CarFront, yol: '/gider/kaza' },
      { id: 'fms_mtv', ad: 'MTV Kaydı', ikon: FileText, yol: '/gider/mtv' },
      { id: 'fms_ogs', ad: 'OGS / HGS Alımı', ikon: Route, yol: '/gider/ogs' },
      { id: 'fms_diger', ad: 'Genel Masraf Girişi', ikon: Coins, yol: '/gider/diger' },
    ],
  },
  { id: 'destek', ad: 'Destek', ikon: Headset },
]

export function gorunurMenu(k: OturumKullanici): MenuMaddesi[] {
  return MENU
    .filter(m => !m.goster || m.goster(k))
    .map(m => (m.alt ? { ...m, alt: m.alt.filter(a => !a.goster || a.goster(k)) } : m))
}
