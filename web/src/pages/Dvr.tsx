import { useEffect, useState } from 'react'
import { ExternalLink, Video } from 'lucide-react'
import { api } from '../api/client.ts'

/**
 * DVR-Kamera (eski menüdeki araç listesi; tıklanınca http://DVRIPNO pencerede açılırdı).
 * DVR arayüzleri çoğunlukla yalnız HTTP çalıştığı ve sayfa içine gömülmeyi engellediği için yeni sekmede açılır.
 */
export default function Dvr() {
  const [liste, setListe] = useState<{ id: number; plaka: string; adres: string }[] | null>(null)
  const [hata, setHata] = useState<string | null>(null)

  useEffect(() => { api.dvrAraclari().then(setListe).catch(e => setHata(e.message)) }, [])

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik"><h2><Video size={20} /> DVR-Kamera</h2></div>
      {hata && <div className="uyari-kutu">{hata}</div>}
      {!liste && !hata && <div className="yapim kucuk"><span className="donen" /></div>}
      {liste && liste.length === 0 && <div className="bilgi-kutu">DVR adresi girilmiş aracınız yok.</div>}
      {liste && liste.length > 0 && (
        <table className="tablo">
          <thead><tr><th>Plaka</th><th>DVR adresi</th><th /></tr></thead>
          <tbody>
            {liste.map(a => (
              <tr key={a.id}>
                <td><strong>{a.plaka}</strong></td>
                <td>{a.adres}</td>
                <td><a href={`http://${a.adres}`} target="_blank" rel="noreferrer"><ExternalLink size={14} /> Kamerayı aç</a></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
