import { useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import type { YonetimAraci } from '../api/client.ts'

/** Yönetim ekranlarının sol paneli: aranabilir araç listesi (tekli seçim). */
export default function AracSecimListesi({ baslik, araclar, seciliId, onSec }: {
  baslik: string
  araclar: YonetimAraci[]
  seciliId: number | null
  onSec: (id: number) => void
}) {
  const [ara, setAra] = useState('')
  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q) || a.cihazId.includes(q)) : araclar
  }, [araclar, ara])

  return (
    <aside className="gecmis-form yonetim-liste">
      <h3>{baslik}</h3>
      <label className="arama-kutu"><Search size={15} /><input placeholder="Plaka ya da cihaz ID" value={ara} onChange={e => setAra(e.target.value)} /></label>
      <ul className="islem-liste uzun">
        {liste.map(a => (
          <li key={a.id} className={a.id === seciliId ? 'secili' : ''} onClick={() => onSec(a.id)}>
            <img src={`/arac-ico/b-${a.ikon}.png`} alt="" />
            <span>{a.plaka}</span><small>{a.cihazId}</small>
          </li>
        ))}
        {!liste.length && <li className="bos">Araç bulunamadı.</li>}
      </ul>
    </aside>
  )
}
