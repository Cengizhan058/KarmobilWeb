import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { CircleAlert, FileSpreadsheet, Lock, Search, Table, type LucideIcon } from 'lucide-react'
import { api } from '../api/client.ts'
import { csvIndir } from '../lib/disaAktar.ts'

type Satir = Record<string, unknown>
type Tanim = { baslik: string; ikon: LucideIcon; aciklama: string; bos: string; getir: () => Promise<Satir[]>; kolonlar: [string, string][] }

// Ayarlar menüsündeki salt okunur listeler: /liste/{anahtar}
const LISTELER: Record<string, Tanim> = {
  'aktif-bildirimler': {
    baslik: 'Aktif bildirimler', ikon: CircleAlert,
    aciklama: 'Araçlarınızda açık olan alarm/bildirim tanımları.',
    bos: 'Aktif bildirim tanımı yok.',
    getir: api.aktifBildirimler,
    kolonlar: [['plaka', 'Plaka'], ['alarm', 'Alarm türü'], ['durum', 'Durum'], ['saatAraligi', 'Saat aralığı']],
  },
  'program-tablosu': {
    baslik: 'Mevcut program tablosu', ikon: Table,
    aciklama: 'Cihazlara en son gönderilen konum gönderme programları. "Varsayılan": cihaz fabrika ayarında.',
    bos: 'Araç yok.',
    getir: api.programTablosu,
    kolonlar: [['plaka', 'Plaka'], ['hareketliSn', 'Hareketli (sn)'], ['dururkenSn', 'Dururken (sn)'], ['mesafeKm', 'Mesafeye göre (km)'], ['hizLimiti', 'Hız limiti (km/s)'], ['baglanti', 'Bağlantı']],
  },
  'blokeli-araclar': {
    baslik: 'Motor blokesi aktif araçlar', ikon: Lock,
    aciklama: 'Motor blokajı şu anda açık olan araçlar.',
    bos: 'Motor blokesi aktif araç yok.',
    getir: api.blokeliAraclar,
    kolonlar: [['plaka', 'Plaka'], ['cihazId', 'Cihaz ID'], ['gsm', 'GSM']],
  },
}

export default function Liste() {
  const { anahtar = '' } = useParams()
  const tanim = LISTELER[anahtar]
  const [satirlar, setSatirlar] = useState<Satir[] | null>(null)
  const [hata, setHata] = useState<string | null>(null)
  const [ara, setAra] = useState('')

  useEffect(() => {
    setSatirlar(null); setHata(null)
    tanim?.getir().then(setSatirlar).catch(e => setHata(e.message))
  }, [tanim])

  const gorunen = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    if (!satirlar || !q || !tanim) return satirlar ?? []
    return satirlar.filter(s => tanim.kolonlar.some(([k]) => String(s[k] ?? '').toLocaleUpperCase('tr-TR').includes(q)))
  }, [satirlar, ara, tanim])

  if (!tanim) return <div className="tek-sayfa"><div className="uyari-kutu">Liste bulunamadı.</div></div>
  const Ikon = tanim.ikon

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik">
        <h2><Ikon size={20} />{tanim.baslik}</h2>
        <div className="grup-arac-ust">
          <label className="arama-kutu dar"><Search size={15} /><input placeholder="Ara" value={ara} onChange={e => setAra(e.target.value)} /></label>
          {gorunen.length > 0 && (
            <button className="ikincil-dugme" onClick={() => csvIndir(`${anahtar}.csv`, tanim.kolonlar.map(k => k[1]), gorunen.map(s => tanim.kolonlar.map(([k]) => s[k] as string)))}>
              <FileSpreadsheet size={15} />Excel
            </button>
          )}
        </div>
      </div>
      <p className="kucuk-not">{tanim.aciklama}</p>
      {hata && <div className="uyari-kutu">{hata}</div>}
      {!satirlar && !hata && <div className="yapim kucuk"><span className="donen" /></div>}
      {satirlar && satirlar.length === 0 && <div className="bilgi-kutu">{tanim.bos}</div>}
      {gorunen.length > 0 && (
        <table className="tablo">
          <thead><tr>{tanim.kolonlar.map(([k, b]) => <th key={k}>{b}</th>)}</tr></thead>
          <tbody>
            {gorunen.map((s, i) => <tr key={String(s.id ?? i)}>{tanim.kolonlar.map(([k]) => <td key={k}>{String(s[k] ?? '')}</td>)}</tr>)}
          </tbody>
        </table>
      )}
    </div>
  )
}
