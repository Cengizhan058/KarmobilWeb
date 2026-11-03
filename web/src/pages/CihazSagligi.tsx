import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { Activity, FileSpreadsheet, RefreshCw } from 'lucide-react'
import { api, type CihazSagligi as Veri, type CihazSorunu } from '../api/client.ts'
import { tamZaman } from '../lib/arac.ts'
import { csvIndir } from '../lib/disaAktar.ts'

type Seviye = CihazSorunu['seviye']
const SEVIYELER: { kod: Seviye; ad: string; renk: string }[] = [
  { kod: 'kritik', ad: 'Kritik', renk: '#d92d20' },
  { kod: 'uyari', ad: 'Uyarı', renk: '#f79009' },
  { kod: 'bilgi', ad: 'Bilgi', renk: '#2e90fa' },
]
const renk = (s: Seviye) => SEVIYELER.find(x => x.kod === s)!.renk

/**
 * Araçlar → Cihaz Arıza Bildirimleri: son 7 günün verisinden cihaz/montaj sorunları
 * (sökme şüphesi, enerji kesik, sık enerji kesintisi, kontak bağlantısı, km sayacı, kadran km).
 */
export default function CihazSagligi() {
  const [veri, setVeri] = useState<Veri | null>(null)
  const [hata, setHata] = useState<string | null>(null)
  const [secili, setSecili] = useState<Set<Seviye>>(new Set(['kritik', 'uyari']))

  const yukle = () => { setVeri(null); setHata(null); api.cihazSagligi().then(setVeri).catch(e => setHata(e.message)) }
  useEffect(yukle, [])

  const sayilar = useMemo(() => {
    const n: Record<Seviye, number> = { kritik: 0, uyari: 0, bilgi: 0 }
    veri?.araclar.forEach(a => new Set(a.sorunlar.map(s => s.seviye)).forEach(s => n[s]++))
    return n
  }, [veri])

  // Araçlar en ağır sorununa göre sıralanır; yalnız seçili seviyedeki sorunlar gösterilir.
  const gorunen = useMemo(() => (veri?.araclar ?? [])
    .map(a => ({ ...a, sorunlar: a.sorunlar.filter(s => secili.has(s.seviye)) }))
    .filter(a => a.sorunlar.length > 0)
    .sort((x, y) => Math.min(...x.sorunlar.map(s => SEVIYELER.findIndex(v => v.kod === s.seviye)))
      - Math.min(...y.sorunlar.map(s => SEVIYELER.findIndex(v => v.kod === s.seviye))) || x.plaka.localeCompare(y.plaka, 'tr')),
  [veri, secili])

  const degistir = (s: Seviye) => setSecili(o => { const y = new Set(o); if (y.has(s)) y.delete(s); else y.add(s); return y })

  const excel = () => csvIndir('cihaz-ariza-bildirimleri.csv', ['Plaka', 'Cihaz ID', 'Seviye', 'Sorun', 'Ayrıntı', 'Son sinyal'],
    gorunen.flatMap(a => a.sorunlar.map(s => [a.plaka, a.cihazId, SEVIYELER.find(v => v.kod === s.seviye)!.ad, s.baslik, s.ayrinti, tamZaman(a.sonSinyal)])))

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik">
        <h2><Activity size={20} /> Cihaz arıza bildirimleri</h2>
        <div className="grup-arac-ust">
          <button className="ikincil-dugme" onClick={yukle}><RefreshCw size={15} />Yenile</button>
          {gorunen.length > 0 && <button className="ikincil-dugme" onClick={excel}><FileSpreadsheet size={15} />Excel</button>}
        </div>
      </div>
      <p className="kucuk-not">
        Son 7 günün verisine göre: sökülmüş olabilecek cihazlar, enerjisi kesik ya da sık kesilen cihazlar, kontak bağlantısı olmayan
        ya da km saymayan cihazlar. {veri && `${veri.toplamArac} araç tarandı.`}
      </p>
      {hata && <div className="uyari-kutu">{hata}</div>}
      {!veri && !hata && <div className="yapim kucuk"><span className="donen" /></div>}
      {veri && (
        <div className="durum-sayaclari saglik-sayaclari">
          {SEVIYELER.map(s => (
            <button key={s.kod} className={`durum-cip${secili.has(s.kod) ? ' secili' : ''}`} style={{ '--durum': s.renk } as CSSProperties} onClick={() => degistir(s.kod)}>
              <span className="nokta" />{s.ad}<strong>{sayilar[s.kod]}</strong>
            </button>
          ))}
        </div>
      )}
      {veri && gorunen.length === 0 && <div className="bilgi-kutu">Seçili seviyede sorunlu cihaz yok.</div>}
      {gorunen.length > 0 && (
        <table className="tablo">
          <thead><tr><th>Plaka</th><th>Sorun</th><th>Ayrıntı</th><th>Son sinyal</th><th /></tr></thead>
          <tbody>
            {gorunen.map(a => a.sorunlar.map((s, i) => (
              <tr key={`${a.id}-${s.kod}`}>
                {i === 0 && <td rowSpan={a.sorunlar.length}><strong>{a.plaka}</strong><div className="soluk">{a.cihazId}</div></td>}
                <td><span className="durum-rozet" style={{ background: renk(s.seviye) }}>{SEVIYELER.find(v => v.kod === s.seviye)!.ad}</span> {s.baslik}</td>
                <td>{s.ayrinti}</td>
                {i === 0 && <td rowSpan={a.sorunlar.length}>{a.sonSinyal ? tamZaman(a.sonSinyal) : <span className="soluk">Hiç yok</span>}</td>}
                {i === 0 && <td rowSpan={a.sorunlar.length}><Link to={`/gecmis?arac=${a.id}`}>Geçmiş</Link></td>}
              </tr>
            )))}
          </tbody>
        </table>
      )}
    </div>
  )
}
