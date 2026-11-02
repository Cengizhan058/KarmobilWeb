import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { IdCard, Save } from 'lucide-react'
import { api, type YonetimAraci } from '../api/client.ts'
import AracSecimListesi from '../components/AracSecimListesi.tsx'

// Uzun alanlar formda tam satır kaplar.
const GENIS = new Set(['RUHSAT_ADRESI', 'RUHSAT_HACIZ', 'RUHSAT_SOYADIUNVAN'])

/** Araçlar → Ruhsat Bilgileri (eski data/_ruhsatBilgileri.asp). */
export default function Ruhsat() {
  const [params, setParams] = useSearchParams()
  const seciliId = Number(params.get('arac')) || null
  const [araclar, setAraclar] = useState<YonetimAraci[]>([])
  const [tanim, setTanim] = useState<{ ad: string; baslik: string; uz: number }[]>([])
  const [kayit, setKayit] = useState<{ plaka: string | null; alanlar: Record<string, string> } | null>(null)
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  useEffect(() => {
    api.yonetimAraclari().then(setAraclar).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
    api.ruhsatAlanlari().then(setTanim).catch(() => { /* form boş kalır */ })
  }, [])

  useEffect(() => {
    setKayit(null)
    if (seciliId) api.ruhsat(seciliId).then(setKayit).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }, [seciliId])

  const kaydet = async (e: FormEvent) => {
    e.preventDefault()
    if (!kayit || !seciliId) return
    setMesgul(true); setMesaj(null)
    try {
      setMesaj({ tur: 'ok', metin: (await api.ruhsatKaydet(seciliId, kayit.alanlar)).mesaj })
    } catch (err) {
      setMesaj({ tur: 'hata', metin: err instanceof Error ? err.message : 'Kaydedilemedi.' })
    } finally {
      setMesgul(false)
    }
  }

  return (
    <div className="gecmis-sayfa">
      <AracSecimListesi baslik="Araçlar" araclar={araclar} seciliId={seciliId} onSec={id => { setMesaj(null); setParams({ arac: String(id) }) }} />
      <section className="gecmis-icerik yonetim-icerik">
        {!kayit ? (
          <div className="yapim"><IdCard size={36} /><p>{seciliId ? 'Yükleniyor…' : 'Ruhsat bilgilerini görmek için soldan bir araç seçin.'}</p>
            {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          </div>
        ) : (
          <form className="yonetim-form" onSubmit={kaydet}>
            <h2><IdCard size={20} />{kayit.plaka} ruhsat bilgileri</h2>
            {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
            <fieldset>
              <div className="izgara-4">
                {tanim.map(t => (
                  <label key={t.ad} className={'alan' + (GENIS.has(t.ad) ? ' tam-satir' : '')}><span>{t.baslik}</span>
                    <input className="secim" maxLength={t.uz} value={kayit.alanlar[t.ad] ?? ''}
                      onChange={e => setKayit({ ...kayit, alanlar: { ...kayit.alanlar, [t.ad]: e.target.value } })} />
                  </label>
                ))}
              </div>
            </fieldset>
            <button className="ana-dugme genislik-oto" type="submit" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={16} />Kaydet</>}</button>
          </form>
        )}
      </section>
    </div>
  )
}
