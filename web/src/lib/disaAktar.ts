// Excel'in doğrudan açtığı CSV (Türkçe Excel: ; ayırıcı, UTF-8 BOM).
export function csvIndir(dosyaAdi: string, basliklar: string[], satirlar: (string | number | null | undefined)[][]) {
  const hucre = (v: string | number | null | undefined) => {
    const s = v == null ? '' : String(v)
    return /[;"\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
  }
  const metin = [basliklar, ...satirlar].map(r => r.map(hucre).join(';')).join('\r\n')
  const blob = new Blob(['﻿' + metin], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = dosyaAdi
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

/** Dosya adında kullanılabilir hale getirir: "27 BFK 299" → "27-BFK-299" */
export function dosyaAdiParcasi(s: string): string {
  return s.trim().replace(/[^\p{L}\p{N}]+/gu, '-').replace(/^-|-$/g, '')
}
