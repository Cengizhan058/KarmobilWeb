import { useEffect, useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import { api } from '../api/client.ts'

/**
 * Toplu ayar ekranlarının sol paneli: araca tıklayınca ayarları yüklenir (kaynak), işaretli araçların hepsine kaydedilir.
 * Tıklanan araç kendiliğinden işaretlenir.
 */
export default function CokluAracListesi({ kaynak, secili, onKaynak, onSecili, not }: {
  kaynak: number | null
  secili: Set<number>
  onKaynak: (id: number) => void
  onSecili: (s: Set<number>) => void
  not: string
}) {
  const [araclar, setAraclar] = useState<{ id: number; plaka: string }[]>([])
  const [ara, setAra] = useState('')

  useEffect(() => { api.araclar().then(a => setAraclar([...a].sort((x, y) => x.plaka.localeCompare(y.plaka, 'tr')))).catch(() => { /* boş liste */ }) }, [])

  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q)) : araclar
  }, [araclar, ara])

  return (
    <aside className="gecmis-form yonetim-liste">
      <h3>Araçlar</h3>
      <label className="arama-kutu"><Search size={15} /><input placeholder="Plaka ara" value={ara} onChange={e => setAra(e.target.value)} /></label>
      <div className="grup-arac-ust">
        <button type="button" className="ikincil-dugme" onClick={() => onSecili(new Set([...secili, ...liste.map(a => a.id)]))}>Tümünü seç</button>
        <button type="button" className="ikincil-dugme" onClick={() => onSecili(new Set())}>Temizle</button>
      </div>
      <ul className="islem-liste uzun">
        {liste.map(a => (
          <li key={a.id} className={a.id === kaynak ? 'secili' : ''} onClick={() => { onSecili(new Set(secili).add(a.id)); onKaynak(a.id) }}>
            <input type="checkbox" checked={secili.has(a.id)} onClick={e => e.stopPropagation()}
              onChange={e => { const y = new Set(secili); if (e.target.checked) y.add(a.id); else y.delete(a.id); onSecili(y) }} />
            <span>{a.plaka}</span>
          </li>
        ))}
      </ul>
      <p className="kucuk-not">{not}</p>
    </aside>
  )
}
