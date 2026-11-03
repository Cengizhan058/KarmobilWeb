import { useEffect, useState } from 'react'
import { Globe, Headset, MessageCircle, Phone } from 'lucide-react'
import { api } from '../api/client.ts'

type Bilgi = Awaited<ReturnType<typeof api.destek>>

/** Destek (eski _destek.asp): bayi bilgileri (bayiler, BID = karmobil) ve WhatsApp hattı. */
export default function Destek() {
  const [b, setB] = useState<Bilgi | null>(null)
  const [hata, setHata] = useState<string | null>(null)

  useEffect(() => { api.destek().then(setB).catch(e => setHata(e.message)) }, [])

  const wa = b ? `https://wa.me/90${b.whatsapp.replace(/\D/g, '').replace(/^0/, '')}` : ''
  return (
    <div className="tek-sayfa">
      <div className="islem-baslik"><h2><Headset size={20} /> Destek</h2></div>
      {hata && <div className="uyari-kutu">{hata}</div>}
      {b && (
        <section className="gosterge-kart destek-kart">
          <p>Müşteri temsilciniz <strong>{b.unvan ?? 'Karmobil'}</strong></p>
          <ul className="gosterge-liste sade">
            {b.telefon && <li><Phone size={16} /><span>Telefon</span><a href={`tel:${b.telefon.replace(/\s/g, '')}`}><strong>{b.telefon}</strong></a></li>}
            <li><MessageCircle size={16} /><span>WhatsApp</span><a href={wa} target="_blank" rel="noreferrer"><strong>{b.whatsapp}</strong></a></li>
            {b.web && <li><Globe size={16} /><span>Web</span><a href={b.web} target="_blank" rel="noreferrer"><strong>{b.web.replace(/^https?:\/\//, '')}</strong></a></li>}
          </ul>
        </section>
      )}
    </div>
  )
}
