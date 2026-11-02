import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { FileSpreadsheet, TriangleAlert } from 'lucide-react'
import { api, type ArizaliArac } from '../api/client.ts'
import { sure, tamZaman } from '../lib/arac.ts'
import { csvIndir } from '../lib/disaAktar.ts'

const dakikaOnce = (iso: string | null) => (iso ? Math.max(0, Math.round((Date.now() - new Date(iso).getTime()) / 60000)) : null)

/** Araçlar → Arızalı / Montajsız Araçlar: sinyal göndermeyen cihazlar (eski dbo.AktifMi(KONTROLTARIH) = 0). */
export default function ArizaliAraclar() {
  const [liste, setListe] = useState<ArizaliArac[] | null>(null)
  const [hata, setHata] = useState<string | null>(null)

  useEffect(() => { api.arizaliAraclar().then(setListe).catch(e => setHata(e.message)) }, [])

  const excel = () => liste && csvIndir('arizali-araclar.csv',
    ['Plaka', 'Cihaz ID', 'Cihaz tipi', 'Son sinyal', 'Sinyalsiz süre', 'Açıklama'],
    liste.map(a => [a.plaka, a.cihazId, a.cihazTipi, tamZaman(a.sonSinyal), a.sonSinyal ? sure(dakikaOnce(a.sonSinyal)) : 'Hiç sinyal yok', a.aciklama]))

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik">
        <h2><TriangleAlert size={20} /> Arızalı / montajsız araçlar</h2>
        {liste && liste.length > 0 && <button className="ikincil-dugme" onClick={excel}><FileSpreadsheet size={15} />Excel</button>}
      </div>
      <p className="kucuk-not">Bir süredir sinyal göndermeyen ya da henüz montajı yapılmamış cihazlar.</p>
      {hata && <div className="uyari-kutu">{hata}</div>}
      {!liste && !hata && <div className="yapim kucuk"><span className="donen" /></div>}
      {liste && liste.length === 0 && <div className="bilgi-kutu">Arızalı ya da montajsız araç yok.</div>}
      {liste && liste.length > 0 && (
        <table className="tablo">
          <thead><tr><th>Plaka</th><th>Cihaz ID</th><th>Cihaz tipi</th><th>Son sinyal</th><th>Sinyalsiz süre</th><th>Açıklama</th><th /></tr></thead>
          <tbody>
            {liste.map(a => (
              <tr key={a.id}>
                <td><strong>{a.plaka}</strong></td>
                <td>{a.cihazId}</td>
                <td>{a.cihazTipi ?? ''}</td>
                <td>{a.sonSinyal ? tamZaman(a.sonSinyal) : <span className="soluk">Hiç sinyal yok</span>}</td>
                <td>{a.sonSinyal ? sure(dakikaOnce(a.sonSinyal)) : ''}</td>
                <td>{a.aciklama ?? ''}</td>
                <td><Link to={`/gecmis?arac=${a.id}`}>Geçmiş</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
