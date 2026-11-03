import { useEffect, useState } from 'react'
import { Mail, MailOpen, Trash2 } from 'lucide-react'
import { api, type Mesaj } from '../api/client.ts'
import { tamZaman } from '../lib/arac.ts'

/** Üst menüdeki okunmamış sayısı bu olayla yenilenir. */
export const MESAJ_OLAYI = 'km-mesajlar-degisti'

/** Mesajlar (eski: show_messages penceresi). Açılan mesaj okundu sayılır; mesaj silinebilir. */
export default function Mesajlar() {
  const [liste, setListe] = useState<Mesaj[] | null>(null)
  const [secili, setSecili] = useState<Mesaj | null>(null)
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)

  useEffect(() => { api.mesajlar().then(setListe).catch(e => setMesaj({ tur: 'hata', metin: e.message })) }, [])

  const ac = (m: Mesaj) => {
    setSecili(m); setMesaj(null)
    if (m.okundu) return
    api.mesajOkundu(m.id).then(() => {
      setListe(l => l?.map(x => (x.id === m.id ? { ...x, okundu: true } : x)) ?? l)
      window.dispatchEvent(new Event(MESAJ_OLAYI))
    }).catch(() => { /* okundu işareti kalmazsa liste yeniden açılınca yine yeni görünür */ })
  }

  const sil = async (m: Mesaj) => {
    if (!confirm('Mesaj silinsin mi?')) return
    try {
      const r = await api.mesajSil(m.id)
      setListe(l => l?.filter(x => x.id !== m.id) ?? l)
      setSecili(null); setMesaj({ tur: 'ok', metin: r.mesaj })
      window.dispatchEvent(new Event(MESAJ_OLAYI))
    } catch (e) { setMesaj({ tur: 'hata', metin: (e as Error).message }) }
  }

  const yeni = liste?.filter(m => !m.okundu).length ?? 0

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik">
        <h2><Mail size={20} /> Mesajlar{yeni > 0 && <span className="soluk"> · {yeni} okunmamış</span>}</h2>
      </div>
      {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
      {!liste && !mesaj && <div className="yapim kucuk"><span className="donen" /></div>}
      {liste && liste.length === 0 && <div className="bilgi-kutu">Mesajınız yok.</div>}
      {liste && liste.length > 0 && (
        <div className="mesaj-duzen">
          <table className="tablo secilebilir">
            <thead><tr><th>Tarih</th><th>Kimden</th><th>Başlık</th><th>Durum</th></tr></thead>
            <tbody>
              {liste.map(m => (
                <tr key={m.id} className={(m.okundu ? '' : 'kalin ') + (secili?.id === m.id ? 'secili' : '')} onClick={() => ac(m)}>
                  <td>{tamZaman(m.tarih)}</td>
                  <td>{m.kimden ?? ''}</td>
                  <td>{m.baslik ?? ''}</td>
                  <td>{m.okundu ? <span className="soluk"><MailOpen size={14} /> Okundu</span> : <><Mail size={14} /> Yeni</>}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {secili && (
            <article className="mesaj-detay">
              <header>
                <div>
                  <h3>{secili.baslik ?? '(başlıksız)'}</h3>
                  <p className="soluk kucuk-not">{secili.kimden ?? ''} · {tamZaman(secili.tarih)}</p>
                </div>
                <button className="ikincil-dugme" onClick={() => sil(secili)}><Trash2 size={15} />Mesajı sil</button>
              </header>
              {/* Eski mesajlar HTML içerebiliyordu; burada düz metin gösterilir. */}
              <div className="mesaj-govde">{(secili.mesaj ?? '').replace(/<br\s*\/?>/gi, '\n').replace(/<[^>]+>/g, '')}</div>
            </article>
          )}
        </div>
      )}
    </div>
  )
}
