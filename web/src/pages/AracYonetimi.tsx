import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { CarFront, Save, Search } from 'lucide-react'
import { api, type AracKaydi, type YonetimAraci } from '../api/client.ts'

type Mesaj = { tur: 'ok' | 'hata'; metin: string } | null

// Eski formdaki "Başlangıç / Bitiş / Uyarı" satırları.
const BELGELER = [
  { ad: 'Muayene', b: 'muayene1', s: 'muayene2', u: 'muayeneUyari' },
  { ad: 'Sigorta', b: 'sigorta1', s: 'sigorta2', u: 'sigortaUyari' },
  { ad: 'Kasko', b: 'kasko1', s: 'kasko2', u: 'kaskoUyari' },
  { ad: 'Egzoz', b: 'egzoz1', s: 'egzoz2', u: 'egzozUyari' },
] as const

/** Araçlar → Araç Yönetimi (eski vehicle_management penceresi). */
export default function AracYonetimi() {
  const [params, setParams] = useSearchParams()
  const [araclar, setAraclar] = useState<YonetimAraci[]>([])
  const [ikonlar, setIkonlar] = useState<{ no: number; ad: string }[]>([])
  const [ara, setAra] = useState('')
  const [kayit, setKayit] = useState<AracKaydi | null>(null)
  const [mesaj, setMesaj] = useState<Mesaj>(null)
  const [mesgul, setMesgul] = useState(false)
  const seciliId = Number(params.get('arac')) || null

  useEffect(() => {
    api.yonetimAraclari().then(setAraclar).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
    api.aracIkonlari().then(setIkonlar).catch(() => { /* ikon seçimi boş kalır */ })
  }, [])

  useEffect(() => {
    setKayit(null)
    if (!seciliId) return
    api.aracKaydi(seciliId).then(setKayit).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }, [seciliId])

  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q) || a.cihazId.includes(q)) : araclar
  }, [araclar, ara])

  const alan = <K extends keyof AracKaydi>(ad: K, deger: AracKaydi[K]) => setKayit(k => (k ? { ...k, [ad]: deger } : k))

  const kaydet = async (e: FormEvent) => {
    e.preventDefault()
    if (!kayit) return
    setMesgul(true); setMesaj(null)
    try {
      const { id, cihazId: _c, gsm: _g, cihazTipi: _t, kadranTarihi: _k, ...istek } = kayit
      const r = await api.aracKaydet(id, istek)
      setMesaj({ tur: 'ok', metin: r.mesaj })
      setAraclar(a => a.map(x => (x.id === id ? { ...x, plaka: kayit.plaka, ikon: kayit.ikon } : x)))
      setKayit(await api.aracKaydi(id))
    } catch (err) {
      setMesaj({ tur: 'hata', metin: err instanceof Error ? err.message : 'Kaydedilemedi.' })
    } finally {
      setMesgul(false)
    }
  }

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form yonetim-liste">
        <h3>Kayıtlı araçlar</h3>
        <label className="arama-kutu"><Search size={15} /><input placeholder="Plaka ya da cihaz ID" value={ara} onChange={e => setAra(e.target.value)} /></label>
        <ul className="islem-liste uzun">
          {liste.map(a => (
            <li key={a.id} className={a.id === seciliId ? 'secili' : ''} onClick={() => { setMesaj(null); setParams({ arac: String(a.id) }) }}>
              <img src={`/arac-ico/b-${a.ikon}.png`} alt="" />
              <span>{a.plaka}</span><small>{a.cihazId}</small>
            </li>
          ))}
          {!liste.length && <li className="bos">Araç bulunamadı.</li>}
        </ul>
      </aside>

      <section className="gecmis-icerik yonetim-icerik">
        {!kayit ? (
          <div className="yapim"><CarFront size={36} /><p>{seciliId ? 'Yükleniyor…' : 'Düzenlemek için soldan bir araç seçin.'}</p></div>
        ) : (
          <form className="yonetim-form" onSubmit={kaydet}>
            <h2>{kayit.plaka}</h2>
            {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}

            <fieldset>
              <legend>Kayıt bilgileri</legend>
              <div className="izgara-4">
                <Salt baslik="Kayıt no" deger={String(kayit.id)} />
                <Salt baslik="Cihaz ID" deger={kayit.cihazId} />
                <Salt baslik="Araç GSM no" deger={kayit.gsm} />
                <Salt baslik="Cihaz tipi" deger={kayit.cihazTipi} />
              </div>
            </fieldset>

            <fieldset>
              <legend>Araç özellikleri</legend>
              <div className="izgara-4">
                <Girdi baslik="Plaka" deger={kayit.plaka} uz={150} zorunlu onDegis={v => alan('plaka', v)} />
                <Girdi baslik="Araç sahibi" deger={kayit.aracSahibi} uz={150} onDegis={v => alan('aracSahibi', v)} />
                <Girdi baslik="Araç cinsi" deger={kayit.aracCinsi} uz={64} onDegis={v => alan('aracCinsi', v)} />
                <Girdi baslik="Araç markası" deger={kayit.aracMarkasi} uz={64} onDegis={v => alan('aracMarkasi', v)} />
                <Girdi baslik="Araç tipi" deger={kayit.aracTipi} uz={64} onDegis={v => alan('aracTipi', v)} />
                <Girdi baslik="Sürücü" deger={kayit.surucu} uz={150} onDegis={v => alan('surucu', v)} />
                <Girdi baslik="Açıklama" deger={kayit.aciklama} uz={150} onDegis={v => alan('aciklama', v)} />
                <label className="alan"><span>Kadran km</span>
                  <input className="secim" type="number" min={0} value={kayit.kadranKm} onChange={e => alan('kadranKm', Number(e.target.value) || 0)} />
                </label>
              </div>
              <div className="alan"><span>Haritadaki ikon</span>
                <div className="ikon-izgara genis">
                  {ikonlar.map(i => (
                    <button type="button" key={i.no} title={i.ad} className={kayit.ikon === i.no ? 'secili' : ''} onClick={() => alan('ikon', i.no)}>
                      <img src={`/arac-ico/b-${i.no}.png`} alt={i.ad} />
                    </button>
                  ))}
                </div>
              </div>
            </fieldset>

            <fieldset>
              <legend>Belgeler ve bakım uyarıları</legend>
              <table className="belge-tablo">
                <thead><tr><th /><th>Başlangıç</th><th>Bitiş</th><th>Uyarı</th></tr></thead>
                <tbody>
                  {BELGELER.map(b => (
                    <tr key={b.ad}>
                      <th>{b.ad}</th>
                      <td><input className="secim" type="date" value={kayit[b.b] ?? ''} onChange={e => alan(b.b, e.target.value || null)} /></td>
                      <td><input className="secim" type="date" value={kayit[b.s] ?? ''} onChange={e => alan(b.s, e.target.value || null)} /></td>
                      <td><input type="checkbox" checked={kayit[b.u]} onChange={e => alan(b.u, e.target.checked)} /></td>
                    </tr>
                  ))}
                  <tr>
                    <th>Motor bakımı</th>
                    <td colSpan={2}><BakimKm deger={kayit.motorKm} onDegis={v => alan('motorKm', v)} /></td>
                    <td><input type="checkbox" checked={kayit.motorUyari} onChange={e => alan('motorUyari', e.target.checked)} /></td>
                  </tr>
                  <tr>
                    <th>Lastik değişimi</th>
                    <td colSpan={2}><BakimKm deger={kayit.lastikKm} onDegis={v => alan('lastikKm', v)} /></td>
                    <td><input type="checkbox" checked={kayit.lastikUyari} onChange={e => alan('lastikUyari', e.target.checked)} /></td>
                  </tr>
                </tbody>
              </table>
              <p className="kucuk-not">Bakım satırlarına "kaç km sonra" yazılır; boş bırakılırsa mevcut hedef değişmez.</p>
            </fieldset>

            <button className="ana-dugme genislik-oto" type="submit" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={16} />Değişiklikleri kaydet</>}</button>
          </form>
        )}
      </section>
    </div>
  )
}

function Salt({ baslik, deger }: { baslik: string; deger: string | null }) {
  return <label className="alan"><span>{baslik}</span><input className="secim" value={deger ?? ''} readOnly disabled /></label>
}

function Girdi({ baslik, deger, uz, zorunlu, onDegis }: { baslik: string; deger: string | null; uz: number; zorunlu?: boolean; onDegis: (v: string) => void }) {
  return (
    <label className="alan"><span>{baslik}</span>
      <input className="secim" value={deger ?? ''} maxLength={uz} required={zorunlu} onChange={e => onDegis(e.target.value)} />
    </label>
  )
}

function BakimKm({ deger, onDegis }: { deger: number | null; onDegis: (v: number | null) => void }) {
  return (
    <input className="secim" type="number" min={0} placeholder="Kaç km sonra" value={deger ?? ''}
      onChange={e => onDegis(e.target.value === '' ? null : Math.max(0, Number(e.target.value)))} />
  )
}
