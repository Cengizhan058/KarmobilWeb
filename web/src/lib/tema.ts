// Renk temaları. Seçim tarayıcıda saklanır (eski ASP sitesiyle aynı anahtar: km-theme)
// ve <html data-tema="..."> ile uygulanır; renkler index.css'teki değişkenlerdedir.

export type TemaId = 'mavi' | 'yesil' | 'turuncu' | 'bordo' | 'mor' | 'dark'

export const TEMALAR: { id: TemaId; ad: string; ornek: string }[] = [
  { id: 'mavi', ad: 'Mavi', ornek: 'linear-gradient(90deg,#2f86bf,#4aa3d6)' },
  { id: 'yesil', ad: 'Petrol yeşili', ornek: 'linear-gradient(90deg,#0f766e,#14958a)' },
  { id: 'turuncu', ad: 'Turuncu', ornek: 'linear-gradient(90deg,#d9480f,#f08c00)' },
  { id: 'bordo', ad: 'Bordo', ornek: 'linear-gradient(90deg,#8a1538,#b02a52)' },
  { id: 'mor', ad: 'Mor', ornek: 'linear-gradient(90deg,#5b2a86,#7b3fb0)' },
  { id: 'dark', ad: 'Koyu', ornek: 'linear-gradient(90deg,#1b1f27,#323949)' },
]

export function aktifTema(): TemaId {
  try {
    const t = localStorage.getItem('km-theme') as TemaId | null
    if (t && TEMALAR.some(x => x.id === t)) return t
  } catch { /* yok say */ }
  return 'mavi'
}

export function temaUygula(t: TemaId) {
  document.documentElement.dataset.tema = t
  try { localStorage.setItem('km-theme', t) } catch { /* yok say */ }
}
