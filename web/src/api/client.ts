// API istemcisi. Arayüz ve API aynı adresten sunulur (geliştirmede Vite proxy'si).

const TOKEN_ANAHTARI = 'km-token'

export type HaritaSiniri = { x1: number; y1: number; x2: number; y2: number }

export type OturumKullanici = {
  id: number
  kullaniciAdi: string
  adSoyad: string | null
  seviye: string
  firmaKodu: string | null
  aracYonetimi: boolean
  grupYonetimi: boolean
  raporlar: boolean
  cihazProgramlama: boolean
  kullaniciAyarlari: boolean
  harita: boolean
  komut: boolean
  supervisor: boolean
  altKullaniciLimiti: number
  akilliBlokaj: boolean
  haritaSiniri: HaritaSiniri | null
  tema: string | null
  /** 0 plaka, 1 cihaz ID, 2 sürücü, 3 hız (users.GORUNUM) */
  etiket: number
  araclariGrupla: boolean
  arizalilariGizle: boolean
}

export type GorunumAyari = Partial<Pick<OturumKullanici, 'etiket' | 'araclariGrupla' | 'arizalilariGizle'>>

export type Bayi = {
  bid: string
  baslik: string | null
  logo: string | null
  destekTel: string | null
  destekWeb: string | null
}

export type AracDurumu = 'Hareketli' | 'Rolanti' | 'Park' | 'VeriYok'

export type AracKonum = {
  id: number
  cihazId: string
  plaka: string
  durum: AracDurumu
  kontak: boolean
  lat: number | null
  lon: number | null
  hiz: number
  yon: number
  gpsHatasi: boolean
  batarya: string | null
  isi: string | null
  surucu: string | null
  gpsTarihi: string | null
  sinyalTarihi: string | null
  /** Son sinyalden bu yana geçen saat; 72 ve üstü eski sitede 'arızalı' sayılır. */
  sinyalYasiSaat: number | null
  adres: string | null
  cihazTipi: string | null
  /** 0 araç, 1 kişi */
  cihazTuru: number
  grupIdleri: number[]
  dvr: boolean
  motorBlokeli: boolean
  gunlukKm: number | null
  kadranKm: number | null
}

export type Grup = { id: number; ad: string; ustId: number | null }

export type AracDetay = {
  id: number
  cihazId: string
  plaka: string
  cihazTanimi: string
  gsm: string | null
  imei: string | null
  aracSahibi: string | null
  aracCinsi: string | null
  aracMarkasi: string | null
  aracTipi: string | null
  aciklama: string | null
  dvrAdresi: string | null
  batarya: number | null
  motorBlokaj: 'aktif' | 'pasif' | null
  azamiHiz: number | null
  bugunKm: number | null
  kontakAcik: boolean
  sonKontakKapama: string | null
  sonKontakKapamaAdresi: string | null
  parkSuresiDakika: number | null
  sonKontakAcma: string | null
  sonKontakAcmaAdresi: string | null
  kontakAcikSureDakika: number | null
  sonAlarm: string | null
  sonAlarmTarihi: string | null
  sonAlarmAdresi: string | null
  ilkSinyal: string | null
  kayitTarihi: string | null
}

export type GecmisNokta = {
  id: number
  zaman: string
  lat: number
  lon: number
  hiz: number
  yon: number
  kontak: boolean
  raporKodu: string
  durum: string
  alarm: string | null
  adres: string | null
  surucu: string | null
  isi: number | null
  kadranKm: number | null
}

export type Duraklama = { baslangic: string; bitis: string; dakika: number; lat: number; lon: number; adres: string | null; kontak: boolean }

export type Gecmis = {
  aracId: number
  plaka: string
  baslangic: string
  bitis: string
  toplamKm: number
  azamiHiz: number
  noktalar: GecmisNokta[]
  duraklar: Duraklama[]
}

export type RaporParametre = { ad: string; baslik: string; tur: 'sayi' | 'metin' | 'secim' | 'onay'; varsayilan: string | null; secenekler: { deger: string; ad: string }[] | null; kaynak: string | null }

export type RaporTanimi = {
  kod: string
  baslik: string
  arac: 'yok' | 'tek' | 'cok'
  tarih: 'yok' | 'aralik' | 'ay'
  azamiGun: number
  parametreler: RaporParametre[]
}

export type RaporKolon = { ad: string; baslik: string; tur: 'metin' | 'sayi' | 'tarih' | 'sure' | 'km' | 'koordinat' }

export type RaporSonucu = {
  kod: string
  baslik: string
  kolonlar: RaporKolon[]
  satirlar: Record<string, unknown>[]
  ozet: string | null
}

export type HaritaKatmanlari = {
  bolgeler: { id: number; ad: string; renk: string; noktalar: [number, number][] }[]
  katmanlar: {
    id: number; ad: string; renk: string; ikon: string | null; boyut: number; paylasilan: boolean; paylas: boolean
    noktalar: { id: number; baslik: string; aciklama: string | null; lat: number; lon: number; cap: number | null }[]
  }[]
  rotalar: { id: number; ad: string; renk: string; noktalar: [number, number][] }[]
}

export type YonetimAraci = { id: number; cihazId: string; plaka: string; cihazTipi: string | null; ikon: number }

/** Araç yönetimi formu. Tarihler yyyy-MM-dd; motor/lastik "kaç km sonra" (boş = değiştirme). */
export type AracKaydiIstegi = {
  plaka: string; aracSahibi: string | null; aracCinsi: string | null; aracMarkasi: string | null; aracTipi: string | null
  surucu: string | null; aciklama: string | null; ikon: number
  muayene1: string | null; muayene2: string | null; sigorta1: string | null; sigorta2: string | null
  kasko1: string | null; kasko2: string | null; egzoz1: string | null; egzoz2: string | null
  muayeneUyari: boolean; sigortaUyari: boolean; kaskoUyari: boolean; egzozUyari: boolean
  kadranKm: number; motorKm: number | null; motorUyari: boolean; lastikKm: number | null; lastikUyari: boolean
}
export type AracKaydi = AracKaydiIstegi & { id: number; cihazId: string; gsm: string | null; cihazTipi: string | null; kadranTarihi: string | null }

export type YonetimGrubu = { id: number; ad: string; ustId: number | null; araclar: number[] }
export type ArizaliArac = { id: number; cihazId: string; plaka: string; sonSinyal: string | null; ilkSinyal: string | null; cihazTipi: string | null; aciklama: string | null }

export type RfidKart = { id: number; kartNo: string; adSoyad: string | null; ehliyetNo: string | null; gsmNo: string | null; kayitTarihi: string | null }

export class ApiHatasi extends Error {
  durum: number
  kod?: string
  constructor(mesaj: string, durum: number, kod?: string) {
    super(mesaj)
    this.durum = durum
    this.kod = kod
  }
}

export const auth = {
  token: (): string | null => {
    try { return localStorage.getItem(TOKEN_ANAHTARI) } catch { return null }
  },
  isLoggedIn: (): boolean => !!auth.token(),
  kaydet: (token: string) => {
    try { localStorage.setItem(TOKEN_ANAHTARI, token) } catch { /* özel pencere vb. */ }
  },
  cikis: () => {
    try { localStorage.removeItem(TOKEN_ANAHTARI) } catch { /* yok say */ }
  },
}

async function istek<T>(yol: string, secenek: RequestInit = {}): Promise<T> {
  const basliklar = new Headers(secenek.headers)
  if (secenek.body && !basliklar.has('Content-Type')) basliklar.set('Content-Type', 'application/json')
  const token = auth.token()
  if (token) basliklar.set('Authorization', `Bearer ${token}`)

  const yanit = await fetch(yol, { ...secenek, headers: basliklar })

  if (yanit.status === 401) {
    auth.cikis()
    if (!location.pathname.startsWith('/giris')) location.assign('/giris')
    throw new ApiHatasi('Oturum süresi doldu.', 401)
  }

  if (!yanit.ok) {
    let mesaj = 'İstek başarısız oldu.'
    let kod: string | undefined
    try {
      const govde = await yanit.json()
      if (govde?.error) mesaj = govde.error
      kod = govde?.code
    } catch { /* gövde JSON değil */ }
    throw new ApiHatasi(mesaj, yanit.status, kod)
  }

  if (yanit.status === 204) return undefined as T
  return (await yanit.json()) as T
}

export const api = {
  giris: (kullaniciAdi: string, sifre: string) =>
    istek<{ token: string }>('/api/auth/login', { method: 'POST', body: JSON.stringify({ kullaniciAdi, sifre }) }),
  ben: () => istek<OturumKullanici>('/api/auth/ben'),
  bayi: () => istek<Bayi>('/api/bayi'),
  araclar: (grupId?: number | null) => istek<AracKonum[]>('/api/araclar' + (grupId ? `?grupId=${grupId}` : '')),
  gruplar: () => istek<Grup[]>('/api/gruplar'),
  gorunumKaydet: (ayar: GorunumAyari) => istek<void>('/api/ayarlar/gorunum', { method: 'PUT', body: JSON.stringify(ayar) }),
  gecmis: (aracId: number, baslangic: string, bitis: string, poi: boolean) =>
    istek<Gecmis>(`/api/gecmis?aracId=${aracId}&baslangic=${encodeURIComponent(baslangic)}&bitis=${encodeURIComponent(bitis)}&poi=${poi}`),
  haritaKatmanlari: () => istek<HaritaKatmanlari>('/api/harita-katmanlari'),
  raporlar: () => istek<RaporTanimi[]>('/api/raporlar'),
  raporSecenekleri: (kaynak: string) => istek<{ deger: string; ad: string }[]>(`/api/raporlar/secenekler/${kaynak}`),
  rapor: (kod: string, sorgu: URLSearchParams) => istek<RaporSonucu>(`/api/raporlar/${kod}?${sorgu}`),
  aracDetay: (id: number) => istek<AracDetay>(`/api/araclar/${id}/detay`),
  // İşlemler: kullanıcının kendi nokta grupları, noktaları, bölgeleri ve rotaları. Noktalar [[enlem, boylam], ...].
  katmanKaydet: (id: number | null, k: { ad: string; ikon: string; boyut: number; paylas: boolean }) =>
    istek<{ id?: number; mesaj: string }>(id ? `/api/katmanlar/${id}` : '/api/katmanlar', { method: id ? 'PUT' : 'POST', body: JSON.stringify(k) }),
  katmanSil: (id: number) => istek<{ mesaj: string }>(`/api/katmanlar/${id}`, { method: 'DELETE' }),
  noktaKaydet: (katmanId: number, id: number | null, n: { baslik: string; cap: number; lat: number; lon: number }) =>
    istek<{ id?: number; mesaj: string }>(id ? `/api/noktalar/${id}` : `/api/katmanlar/${katmanId}/noktalar`, { method: id ? 'PUT' : 'POST', body: JSON.stringify(n) }),
  noktaSil: (id: number) => istek<{ mesaj: string }>(`/api/noktalar/${id}`, { method: 'DELETE' }),
  sekilKaydet: (tur: 'bolgeler' | 'rotalar', id: number | null, s: { ad: string; renk: string; noktalar: [number, number][] | null }) =>
    istek<{ id?: number; mesaj: string }>(id ? `/api/${tur}/${id}` : `/api/${tur}`, { method: id ? 'PUT' : 'POST', body: JSON.stringify(s) }),
  sekilSil: (tur: 'bolgeler' | 'rotalar', id: number) => istek<{ mesaj: string }>(`/api/${tur}/${id}`, { method: 'DELETE' }),
  // Araçlar menüsü: araç ve grup yönetimi
  aracIkonlari: () => istek<{ no: number; ad: string }[]>('/api/yonetim/arac-ikonlari'),
  yonetimAraclari: () => istek<YonetimAraci[]>('/api/yonetim/araclar'),
  aracKaydi: (id: number) => istek<AracKaydi>(`/api/yonetim/araclar/${id}`),
  aracKaydet: (id: number, k: AracKaydiIstegi) => istek<{ mesaj: string }>(`/api/yonetim/araclar/${id}`, { method: 'PUT', body: JSON.stringify(k) }),
  yonetimGruplari: () => istek<YonetimGrubu[]>('/api/yonetim/gruplar'),
  grupKaydet: (id: number | null, g: { ad: string; ustId: number | null; araclar: number[] }) =>
    istek<{ id: number; mesaj: string }>(id ? `/api/yonetim/gruplar/${id}` : '/api/yonetim/gruplar', { method: id ? 'PUT' : 'POST', body: JSON.stringify(g) }),
  grupSil: (id: number) => istek<{ mesaj: string }>(`/api/yonetim/gruplar/${id}`, { method: 'DELETE' }),
  arizaliAraclar: () => istek<ArizaliArac[]>('/api/yonetim/arizali-araclar'),
  // Ayarlar: ruhsat, mesai saatleri, RF-ID kartları
  ruhsatAlanlari: () => istek<{ ad: string; baslik: string; uz: number }[]>('/api/ayarlar/ruhsat-alanlari'),
  ruhsat: (id: number) => istek<{ plaka: string | null; alanlar: Record<string, string> }>(`/api/ayarlar/ruhsat/${id}`),
  ruhsatKaydet: (id: number, alanlar: Record<string, string>) => istek<{ mesaj: string }>(`/api/ayarlar/ruhsat/${id}`, { method: 'PUT', body: JSON.stringify(alanlar) }),
  /** 14 değer: Pazartesi başlangıç, bitiş, Salı ..., Pazar bitiş; "-" = mesai yok. */
  mesai: (id: number) => istek<string[]>(`/api/ayarlar/mesai/${id}`),
  mesaiKaydet: (araclar: number[], saatler: string[]) => istek<{ mesaj: string }>('/api/ayarlar/mesai', { method: 'PUT', body: JSON.stringify({ araclar, saatler }) }),
  rfidKartlari: () => istek<RfidKart[]>('/api/ayarlar/rfid'),
  rfidKaydet: (id: number, k: { adSoyad: string; ehliyetNo: string; gsmNo: string }) => istek<{ mesaj: string }>(`/api/ayarlar/rfid/${id}`, { method: 'PUT', body: JSON.stringify(k) }),
  // Gerçek cihaza komut gönderir (cihazkomut tablosuna SK yazılır).
  anlikKonumIste: (id: number) => istek<{ mesaj: string }>(`/api/araclar/${id}/anlik-konum`, { method: 'POST' }),
}
