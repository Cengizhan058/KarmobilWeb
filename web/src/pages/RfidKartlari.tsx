import { useEffect, useMemo, useState } from 'react'
import { BadgeCheck, Save, Search } from 'lucide-react'
import { api, type RfidKart } from '../api/client.ts'
import { tamZaman } from '../lib/arac.ts'

type Duzen = { adSoyad: string; ehliyetNo: string; gsmNo: string }

/**
 * Ayarlar → RF-ID Kart Yönetimi (eski _getRfIdCards / _prcRFID). Kartlar cihaz okuttukça oluşur;
 * burada yalnız sürücü adı, ehliyet ve GSM bilgisi düzenlenir.
 */
export default function RfidKartlari() {
  const [kartlar, setKartlar] = useState<RfidKart[] | null>(null)
  const [ara, setAra] = useState('')
  const [duzen, setDuzen] = useState<Record<number, Duzen>>({})
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState<number | null>(null)

  useEffect(() => { api.rfidKartlari().then(setKartlar).catch(e => setMesaj({ tur: 'hata', metin: e.message })) }, [])

  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    if (!kartlar || !q) return kartlar ?? []
    return kartlar.filter(k => k.kartNo.toLocaleUpperCase('tr-TR').includes(q) || (k.adSoyad ?? '').toLocaleUpperCase('tr-TR').includes(q))
  }, [kartlar, ara])

  const deger = (k: RfidKart): Duzen => duzen[k.id] ?? { adSoyad: k.adSoyad ?? '', ehliyetNo: k.ehliyetNo ?? '', gsmNo: k.gsmNo ?? '' }
  const degistir = (k: RfidKart, alan: keyof Duzen, v: string) => setDuzen(d => ({ ...d, [k.id]: { ...deger(k), [alan]: v } }))

  const kaydet = async (k: RfidKart) => {
    const d = deger(k)
    setMesgul(k.id); setMesaj(null)
    try {
      const r = await api.rfidKaydet(k.id, d)
      setKartlar(l => l?.map(x => (x.id === k.id ? { ...x, ...d } : x)) ?? l)
      setDuzen(x => { const y = { ...x }; delete y[k.id]; return y })
      setMesaj({ tur: 'ok', metin: `${k.kartNo}: ${r.mesaj}` })
    } catch (e) {
      setMesaj({ tur: 'hata', metin: e instanceof Error ? e.message : 'Kaydedilemedi.' })
    } finally {
      setMesgul(null)
    }
  }

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik">
        <h2><BadgeCheck size={20} />RF-ID kart yönetimi</h2>
        <label className="arama-kutu dar"><Search size={15} /><input placeholder="Kart no ya da ad" value={ara} onChange={e => setAra(e.target.value)} /></label>
      </div>
      <p className="kucuk-not">Kartlar araçtaki okuyucuya ilk okutulduğunda listeye eklenir. Sürücü bilgilerini girip satırı kaydedin.</p>
      {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
      {!kartlar && !mesaj && <div className="yapim kucuk"><span className="donen" /></div>}
      {kartlar && kartlar.length === 0 && <div className="bilgi-kutu">Henüz okutulmuş RF-ID kart yok.</div>}
      {liste.length > 0 && (
        <table className="tablo duzenlenebilir">
          <thead><tr><th>Kart no</th><th>Sürücü adı soyadı</th><th>Ehliyet no</th><th>GSM no</th><th>İlk okutma</th><th /></tr></thead>
          <tbody>
            {liste.map(k => {
              const d = deger(k)
              return (
                <tr key={k.id}>
                  <td><strong>{k.kartNo}</strong></td>
                  <td><input className="secim" maxLength={128} value={d.adSoyad} onChange={e => degistir(k, 'adSoyad', e.target.value)} /></td>
                  <td><input className="secim" maxLength={128} value={d.ehliyetNo} onChange={e => degistir(k, 'ehliyetNo', e.target.value)} /></td>
                  <td><input className="secim" maxLength={128} value={d.gsmNo} onChange={e => degistir(k, 'gsmNo', e.target.value)} /></td>
                  <td className="soluk">{tamZaman(k.kayitTarihi)}</td>
                  <td>
                    <button className="ikincil-dugme" disabled={!duzen[k.id] || mesgul === k.id} onClick={() => kaydet(k)}>
                      {mesgul === k.id ? <span className="donen" /> : <><Save size={14} />Kaydet</>}
                    </button>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
    </div>
  )
}
