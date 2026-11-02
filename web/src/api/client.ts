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
}
