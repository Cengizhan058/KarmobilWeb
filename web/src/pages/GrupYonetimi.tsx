import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Plus, Save, Search, Trash2, Users, X } from 'lucide-react'
import { api, type AracKonum, type YonetimGrubu } from '../api/client.ts'

type Duzen = { id: number | null; ad: string; ustId: number | null; araclar: Set<number> }

/** Araçlar → Grup Yönetimi (eski filo penceresi: _getFleets / _prcFleets). */
export default function GrupYonetimi() {
  const [gruplar, setGruplar] = useState<YonetimGrubu[]>([])
  const [araclar, setAraclar] = useState<AracKonum[]>([])
  const [duzen, setDuzen] = useState<Duzen | null>(null)
  const [ara, setAra] = useState('')
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  const yukle = () => api.yonetimGruplari().then(setGruplar).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  useEffect(() => {
    yukle()
    api.araclar().then(a => setAraclar([...a].sort((x, y) => x.plaka.localeCompare(y.plaka, 'tr')))).catch(() => { /* liste boş kalır */ })
  }, [])

  const plaka = useMemo(() => new Map(araclar.map(a => [a.id, a.plaka])), [araclar])
  const grupAdi = useMemo(() => new Map(gruplar.map(g => [g.id, g.ad])), [gruplar])
  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q)) : araclar
  }, [araclar, ara])

  const sec = (g: YonetimGrubu) => { setMesaj(null); setDuzen({ id: g.id, ad: g.ad, ustId: g.ustId, araclar: new Set(g.araclar) }) }
  const yeni = () => { setMesaj(null); setDuzen({ id: null, ad: '', ustId: null, araclar: new Set() }) }

  const aracDegistir = (id: number, secili: boolean) => setDuzen(d => {
    if (!d) return d
    const y = new Set(d.araclar)
    if (secili) y.add(id); else y.delete(id)
    return { ...d, araclar: y }
  })
  const hepsi = (secili: boolean) => setDuzen(d => {
    if (!d) return d
    const y = new Set(d.araclar)
    liste.forEach(a => (secili ? y.add(a.id) : y.delete(a.id)))
    return { ...d, araclar: y }
  })

  const calistir = async (is_: () => Promise<{ id?: number; mesaj: string }>, sonra: (id?: number) => void) => {
    setMesgul(true); setMesaj(null)
    try {
      const r = await is_()
      setMesaj({ tur: 'ok', metin: r.mesaj })
      await yukle()
      sonra(r.id)
    } catch (e) {
      setMesaj({ tur: 'hata', metin: e instanceof Error ? e.message : 'İşlem başarısız.' })
    } finally {
      setMesgul(false)
    }
  }

  const kaydet = (e: FormEvent) => {
    e.preventDefault()
    if (!duzen) return
    const d = duzen
    calistir(() => api.grupKaydet(d.id, { ad: d.ad, ustId: d.ustId, araclar: [...d.araclar] }), id => { if (id) setDuzen({ ...d, id }) })
  }

  const sil = () => {
    if (!duzen?.id || !confirm(`"${duzen.ad}" grubu silinsin mi? Araçlar silinmez, yalnız gruptan çıkar.`)) return
    const id = duzen.id
    calistir(() => api.grupSil(id), () => setDuzen(null))
  }

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form yonetim-liste">
        <div className="islem-baslik">
          <h3>Gruplar / filolar</h3>
          <button className="ikincil-dugme" onClick={yeni}><Plus size={15} />Yeni grup</button>
        </div>
        <ul className="islem-liste uzun">
          {gruplar.map(g => (
            <li key={g.id} className={duzen?.id === g.id ? 'secili' : ''} onClick={() => sec(g)}>
              <Users size={14} className="soluk" />
              <span>{g.ustId ? <span className="soluk">{grupAdi.get(g.ustId) ?? ''} / </span> : null}{g.ad}</span>
              <small>{g.araclar.length}</small>
            </li>
          ))}
          {!gruplar.length && <li className="bos">Henüz grup yok.</li>}
        </ul>
      </aside>

      <section className="gecmis-icerik yonetim-icerik">
        {!duzen ? (
          <div className="yapim">
            <Users size={36} />
            <p>Düzenlemek için soldan bir grup seçin ya da yeni grup ekleyin.</p>
            {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          </div>
        ) : (
          <form className="yonetim-form" onSubmit={kaydet}>
            <div className="islem-baslik">
              <h2>{duzen.id ? 'Grubu düzenle' : 'Yeni grup'}</h2>
              <button type="button" className="ikon-dugme" title="Kapat" onClick={() => setDuzen(null)}><X size={18} /></button>
            </div>
            {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}

            <div className="izgara-2">
              <label className="alan"><span>Grup adı</span>
                <input className="secim" value={duzen.ad} maxLength={64} required autoFocus onChange={e => setDuzen({ ...duzen, ad: e.target.value })} />
              </label>
              <label className="alan"><span>Üst grup</span>
                <select className="secim" value={duzen.ustId ?? ''} onChange={e => setDuzen({ ...duzen, ustId: e.target.value ? Number(e.target.value) : null })}>
                  <option value="">Yok (ana grup)</option>
                  {gruplar.filter(g => g.id !== duzen.id).map(g => <option key={g.id} value={g.id}>{g.ad}</option>)}
                </select>
              </label>
            </div>

            <fieldset>
              <legend>Gruptaki araçlar ({duzen.araclar.size})</legend>
              <div className="grup-arac-ust">
                <label className="arama-kutu"><Search size={15} /><input placeholder="Plaka ara" value={ara} onChange={e => setAra(e.target.value)} /></label>
                <button type="button" className="ikincil-dugme" onClick={() => hepsi(true)}>Tümünü seç</button>
                <button type="button" className="ikincil-dugme" onClick={() => hepsi(false)}>Temizle</button>
              </div>
              <div className="grup-araclar">
                {liste.map(a => (
                  <label key={a.id} className="onay-satir">
                    <input type="checkbox" checked={duzen.araclar.has(a.id)} onChange={e => aracDegistir(a.id, e.target.checked)} />{a.plaka}
                  </label>
                ))}
              </div>
              {[...duzen.araclar].some(id => !plaka.has(id)) && (
                <p className="kucuk-not">Grupta artık erişiminiz olmayan araçlar var; kaydedince gruptan çıkarılır.</p>
              )}
            </fieldset>

            <div className="islem-dugmeler genislik-oto">
              <button className="ana-dugme" type="submit" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={16} />Kaydet</>}</button>
              {duzen.id && <button type="button" className="ikincil-dugme tehlike" disabled={mesgul} onClick={sil}><Trash2 size={15} />Sil</button>}
            </div>
          </form>
        )}
      </section>
    </div>
  )
}
