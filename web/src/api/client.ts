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
}

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
  adres: string | null
  cihazTipi: string | null
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

  return (await yanit.json()) as T
}

export const api = {
  giris: (kullaniciAdi: string, sifre: string) =>
    istek<{ token: string }>('/api/auth/login', { method: 'POST', body: JSON.stringify({ kullaniciAdi, sifre }) }),
  ben: () => istek<OturumKullanici>('/api/auth/ben'),
  bayi: () => istek<Bayi>('/api/bayi'),
  araclar: (grupId?: number | null) => istek<AracKonum[]>('/api/araclar' + (grupId ? `?grupId=${grupId}` : '')),
  gruplar: () => istek<Grup[]>('/api/gruplar'),
  aracDetay: (id: number) => istek<AracDetay>(`/api/araclar/${id}/detay`),
  // Gerçek cihaza komut gönderir (cihazkomut tablosuna SK yazılır).
  anlikKonumIste: (id: number) => istek<{ mesaj: string }>(`/api/araclar/${id}/anlik-konum`, { method: 'POST' }),
}
