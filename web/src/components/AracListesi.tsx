import { useMemo, useState } from 'react'
import { PanelLeftClose, Search, X } from 'lucide-react'
import type { AracDurumu, AracKonum, Grup } from '../api/client.ts'
import { DURUMLAR, DURUM_SIRASI, aramaMetni, kisaZaman, temizAdres } from '../lib/arac.ts'

type Props = {
  araclar: AracKonum[]
  gruplar: Grup[]
  grupId: number | null
  onGrup: (id: number | null) => void
  seciliId: number | null
  onSec: (a: AracKonum) => void
  durumFiltre: Set<AracDurumu>
  onDurumFiltre: (s: Set<AracDurumu>) => void
  onKapat: () => void
}

export default function AracListesi({ araclar, gruplar, grupId, onGrup, seciliId, onSec, durumFiltre, onDurumFiltre, onKapat }: Props) {
  const [arama, setArama] = useState('')

  const sayilar = useMemo(() => {
    const s: Record<AracDurumu, number> = { Hareketli: 0, Rolanti: 0, Park: 0, VeriYok: 0 }
    for (const a of araclar) s[a.durum]++
    return s
  }, [araclar])

  const liste = useMemo(() => {
    const q = aramaMetni(arama.trim())
    return araclar.filter(a =>
      (durumFiltre.size === 0 || durumFiltre.has(a.durum)) &&
      (!q || aramaMetni(`${a.plaka} ${a.surucu ?? ''} ${a.cihazId}`).includes(q)))
  }, [araclar, arama, durumFiltre])

  const durumDegistir = (d: AracDurumu) => {
    const yeni = new Set(durumFiltre)
    if (yeni.has(d)) yeni.delete(d)
    else yeni.add(d)
    onDurumFiltre(yeni)
  }

  return (
    <aside className="arac-paneli">
      <div className="panel-ust">
        <div className="panel-baslik-satir">
          <h3>Araçlar <span className="sayi">{araclar.length}</span></h3>
          {gruplar.length > 0 && (
            <select className="grup-secim" value={grupId ?? ''} onChange={e => onGrup(e.target.value ? Number(e.target.value) : null)}>
              <option value="">Tüm gruplar</option>
              {gruplar.map(g => <option key={g.id} value={g.id}>{g.ad}</option>)}
            </select>
          )}
          <button className="ikon-dugme panel-kapat" title="Listeyi gizle" onClick={onKapat}>
            <PanelLeftClose size={18} />
          </button>
        </div>

        <div className="durum-sayaclari">
          {DURUM_SIRASI.map(d => (
            <button
              key={d}
              className={'durum-cip' + (durumFiltre.has(d) ? ' secili' : '')}
              style={{ ['--durum' as string]: DURUMLAR[d].renk }}
              onClick={() => durumDegistir(d)}
              title={`${DURUMLAR[d].ad} araçları göster/gizle`}
            >
              <span className="nokta" />
              {DURUMLAR[d].ad}
              <strong>{sayilar[d]}</strong>
            </button>
          ))}
        </div>

        <div className="arama">
          <Search size={15} />
          <input placeholder="Plaka, sürücü ara" value={arama} onChange={e => setArama(e.target.value)} />
          {arama && <button className="girdi-dugme" onClick={() => setArama('')} title="Temizle"><X size={14} /></button>}
        </div>
      </div>

      <div className="arac-liste">
        {liste.map(a => {
          const adres = temizAdres(a.adres)
          return (
            <button key={a.id} className={'arac-satir' + (a.id === seciliId ? ' secili' : '')} onClick={() => onSec(a)}>
              <span className="nokta" style={{ background: DURUMLAR[a.durum].renk }} />
              <span className="arac-govde">
                <span className="satir1">
                  <strong>{a.plaka}</strong>
                  {a.surucu && <span className="surucu">{a.surucu}</span>}
                  <span className="zaman">{kisaZaman(a.sinyalTarihi)}</span>
                </span>
                <span className="satir2">
                  <span className="adres" title={adres ?? ''}>{adres ?? 'Konum bilgisi yok'}</span>
                  {a.hiz > 0 && <span className="etiket hiz">{Math.round(a.hiz)} km/s</span>}
                  {a.gunlukKm != null && <span className="etiket">{Math.round(a.gunlukKm)} km</span>}
                </span>
              </span>
            </button>
          )
        })}
        {liste.length === 0 && <div className="panel-bos">Gösterilecek araç yok.</div>}
      </div>
    </aside>
  )
}
