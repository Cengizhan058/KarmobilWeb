import type { AracDurumu } from '../api/client.ts'

// Eski sitedeki renk anlamları korunur: hareketli mavi, duran kırmızı, rölanti sarı, veri yok gri.
export const DURUMLAR: Record<AracDurumu, { ad: string; renk: string }> = {
  Hareketli: { ad: 'Hareketli', renk: '#2b8fd6' },
  Park: { ad: 'Duran', renk: '#d9534f' },
  Rolanti: { ad: 'Rölanti', renk: '#e0a526' },
  VeriYok: { ad: 'Veri yok', renk: '#8a919e' },
}

export const DURUM_SIRASI: AracDurumu[] = ['Hareketli', 'Rolanti', 'Park', 'VeriYok']

const tarihBicimi = new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
const saatBicimi = new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit' })
const tamBicim = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'medium' })

/** Bugünse "16:13", değilse "31.10 16:54". */
export function kisaZaman(iso: string | null): string {
  if (!iso) return '-'
  const d = new Date(iso)
  const bugun = new Date()
  const ayniGun = d.toDateString() === bugun.toDateString()
  return ayniGun ? saatBicimi.format(d) : tarihBicimi.format(d)
}

export function tamZaman(iso: string | null): string {
  return iso ? tamBicim.format(new Date(iso)) : '-'
}

/** Türkçe büyük harfe çevirip karşılaştırır (eski sitedeki strToUpper ile aynı amaç). */
export function aramaMetni(s: string | null | undefined): string {
  return (s ?? '').toLocaleUpperCase('tr-TR')
}

/** Bozuk karakterler API'de temizlenir (AracEndpoints.MetinTemizle); burası yedek. */
export function temizAdres(s: string | null): string | null {
  if (!s) return s
  return s.normalize('NFC').replace(/[\u0080-\u009f​-‏﻿]/g, '')
}
