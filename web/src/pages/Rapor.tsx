import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { ArrowDown, ArrowUp, FileSpreadsheet, MapPin, Printer, Search, Hammer } from 'lucide-react'
import { api, type AracKonum, type RaporKolon, type RaporSonucu, type RaporTanimi } from '../api/client.ts'
import { sure } from '../lib/arac.ts'
import { csvIndir, dosyaAdiParcasi } from '../lib/disaAktar.ts'

const tam = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'medium' })
const iki = (n: number) => String(n).padStart(2, '0')
const yerelGirdi = (d: Date) => `${d.getFullYear()}-${iki(d.getMonth() + 1)}-${iki(d.getDate())}T${iki(d.getHours())}:${iki(d.getMinutes())}`

// Tanımlar bir kez alınır; menüden rapordan rapora geçerken tekrar istenmez.
let tanimOnbellek: Promise<RaporTanimi[]> | null = null
const tanimlar = () => (tanimOnbellek ??= api.raporlar().catch(e => { tanimOnbellek = null; throw e }))

function hucreMetni(v: unknown, k: RaporKolon): string {
  if (v == null || v === '') return ''
  switch (k.tur) {
    case 'tarih': return tam.format(new Date(String(v)))
    case 'sure': return sure(Number(v))
    case 'km': return `${Number(v).toLocaleString('tr-TR', { maximumFractionDigits: 2 })} km`
    case 'sayi': return Number(v).toLocaleString('tr-TR', { maximumFractionDigits: 2 })
    default: return String(v)
  }
}

export default function Rapor() {
  const { kod = '' } = useParams()
  const [tanim, setTanim] = useState<RaporTanimi | null | undefined>(undefined)
  const [araclar, setAraclar] = useState<AracKonum[]>([])
  const [tekArac, setTekArac] = useState<number | null>(null)
  const [cokAraclar, setCokAraclar] = useState<Set<number>>(new Set())
  const [baslangic, setBaslangic] = useState(() => { const d = new Date(); d.setHours(0, 0, 0, 0); return yerelGirdi(d) })
  const [bitis, setBitis] = useState(() => yerelGirdi(new Date()))
  const [ay, setAy] = useState(() => { const d = new Date(); return `${d.getFullYear()}-${iki(d.getMonth() + 1)}` })
  const [ekstra, setEkstra] = useState<Record<string, string>>({})

  const [sonuc, setSonuc] = useState<RaporSonucu | null>(null)
  const [yukleniyor, setYukleniyor] = useState(false)
  const [hata, setHata] = useState<string | null>(null)
  const [filtre, setFiltre] = useState('')
  const [siralama, setSiralama] = useState<{ ad: string; artan: boolean } | null>(null)

  useEffect(() => { api.araclar().then(setAraclar).catch(() => { /* boş liste */ }) }, [])

  useEffect(() => {
    setTanim(undefined); setSonuc(null); setHata(null); setFiltre(''); setSiralama(null)
    tanimlar().then(list => {
      const t = list.find(x => x.kod.toLowerCase() === kod.toLowerCase()) ?? null
      setTanim(t)
      if (t) setEkstra(Object.fromEntries(t.parametreler.map(p => [p.ad, p.varsayilan ?? ''])))
    }).catch(e => setHata(e instanceof Error ? e.message : 'Rapor tanımları alınamadı.'))
  }, [kod])

  const calistir = async (e: FormEvent) => {
    e.preventDefault()
    if (!tanim) return
    const q = new URLSearchParams()
    if (tanim.arac === 'tek') {
      if (!tekArac) { setHata('Araç seçiniz.'); return }
      q.set('araclar', String(tekArac))
    }
    if (tanim.arac === 'cok' && cokAraclar.size > 0) q.set('araclar', [...cokAraclar].join(','))
    if (tanim.tarih === 'aralik') { q.set('baslangic', `${baslangic}:00`); q.set('bitis', `${bitis}:00`) }
    if (tanim.tarih === 'ay') q.set('ay', ay)
    for (const [k, v] of Object.entries(ekstra)) q.set(`p_${k}`, v)

    setHata(null); setYukleniyor(true)
    try {
      setSonuc(await api.rapor(tanim.kod, q))
    } catch (err) {
      setHata(err instanceof Error ? err.message : 'Rapor alınamadı.')
    } finally {
      setYukleniyor(false)
    }
  }

  const satirlar = useMemo(() => {
    if (!sonuc) return []
    const q = filtre.trim().toLocaleUpperCase('tr-TR')
    let list = q
      ? sonuc.satirlar.filter(s => sonuc.kolonlar.some(k => hucreMetni(s[k.ad], k).toLocaleUpperCase('tr-TR').includes(q)))
      : sonuc.satirlar
    if (siralama) {
      const kolon = sonuc.kolonlar.find(k => k.ad === siralama.ad)
      list = [...list].sort((a, b) => {
        const x = a[siralama.ad], y = b[siralama.ad]
        const sayisal = kolon && ['sayi', 'sure', 'km'].includes(kolon.tur)
        const fark = kolon?.tur === 'tarih' ? new Date(String(x)).getTime() - new Date(String(y)).getTime()
          : sayisal ? Number(x ?? 0) - Number(y ?? 0)
          : String(x ?? '').localeCompare(String(y ?? ''), 'tr')
        return siralama.artan ? fark : -fark
      })
    }
    return list
  }, [sonuc, filtre, siralama])

  const excel = () => {
    if (!sonuc) return
    csvIndir(`${dosyaAdiParcasi(sonuc.baslik)}-${new Date().toISOString().slice(0, 10)}.csv`,
      sonuc.kolonlar.map(k => k.baslik),
      satirlar.map(s => sonuc.kolonlar.map(k => (k.tur === 'sayi' || k.tur === 'km') ? (s[k.ad] as number | null) : hucreMetni(s[k.ad], k))))
  }

  if (tanim === undefined && !hata) return <div className="tam-ekran-mesaj">Yükleniyor…</div>
  if (tanim === null) return (
    <div className="yapim"><Hammer size={40} /><h2>Bu rapor</h2><p>Bu rapor yeni arayüze henüz taşınmadı.</p></div>
  )

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form">
        {tanim && (
          <form onSubmit={calistir}>
            <h3>{tanim.baslik}</h3>

            {tanim.arac === 'tek' && (
              <label className="alan">
                <span>Araç</span>
                <select className="secim" value={tekArac ?? ''} onChange={e => setTekArac(e.target.value ? Number(e.target.value) : null)}>
                  <option value="">Araç seçiniz</option>
                  {araclar.map(a => <option key={a.id} value={a.id}>{a.plaka}</option>)}
                </select>
              </label>
            )}

            {tanim.arac === 'cok' && (
              <div className="alan">
                <span>Araçlar <small className="soluk">({cokAraclar.size === 0 ? 'tümü' : `${cokAraclar.size} seçili`})</small></span>
                <div className="coklu-secim">
                  {araclar.map(a => (
                    <label key={a.id} className="onay-satir">
                      <input type="checkbox" checked={cokAraclar.has(a.id)} onChange={e => {
                        const y = new Set(cokAraclar)
                        if (e.target.checked) y.add(a.id); else y.delete(a.id)
                        setCokAraclar(y)
                      }} />
                      {a.plaka}
                    </label>
                  ))}
                </div>
              </div>
            )}

            {tanim.tarih === 'aralik' && <>
              <label className="alan"><span>Başlangıç</span><input className="secim" type="datetime-local" value={baslangic} onChange={e => setBaslangic(e.target.value)} required /></label>
              <label className="alan"><span>Bitiş</span><input className="secim" type="datetime-local" value={bitis} onChange={e => setBitis(e.target.value)} required /></label>
              {tanim.azamiGun > 0 && <p className="soluk kucuk-not">En fazla {tanim.azamiGun} günlük aralık.</p>}
            </>}

            {tanim.tarih === 'ay' && (
              <label className="alan"><span>Ay</span><input className="secim" type="month" value={ay} onChange={e => setAy(e.target.value)} required /></label>
            )}

            {tanim.parametreler.map(p => p.tur === 'onay' ? (
              <label key={p.ad} className="onay-satir">
                <input type="checkbox" checked={ekstra[p.ad] === '1'} onChange={e => setEkstra({ ...ekstra, [p.ad]: e.target.checked ? '1' : '0' })} />
                {p.baslik}
              </label>
            ) : (
              <label key={p.ad} className="alan">
                <span>{p.baslik}</span>
                {p.tur === 'secim' && p.secenekler ? (
                  <select className="secim" value={ekstra[p.ad] ?? ''} onChange={e => setEkstra({ ...ekstra, [p.ad]: e.target.value })}>
                    {p.secenekler.map(s => <option key={s.deger} value={s.deger}>{s.ad}</option>)}
                  </select>
                ) : (
                  <input className="secim" type={p.tur === 'sayi' ? 'number' : 'text'} value={ekstra[p.ad] ?? ''} onChange={e => setEkstra({ ...ekstra, [p.ad]: e.target.value })} />
                )}
              </label>
            ))}

            <button className="ana-dugme" type="submit" disabled={yukleniyor}>
              {yukleniyor ? <span className="donen" /> : <><Search size={16} />Listele</>}
            </button>
          </form>
        )}
        {hata && <div className="uyari-kutu">{hata}</div>}
        {sonuc?.ozet && <div className="bilgi-bildirim">{sonuc.ozet}</div>}
      </aside>

      <section className="gecmis-icerik">
        <div className="gecmis-ust rapor-ust">
          <div className="arama rapor-arama">
            <Search size={15} />
            <input placeholder="Tabloda ara" value={filtre} onChange={e => setFiltre(e.target.value)} disabled={!sonuc} />
          </div>
          {sonuc && (
            <div className="gecmis-araclar">
              <span className="soluk">{satirlar.length} satır</span>
              <button className="ikincil-dugme" onClick={excel}><FileSpreadsheet size={16} />Excel</button>
              <button className="ikincil-dugme" onClick={() => window.print()}><Printer size={16} />Yazdır / PDF</button>
            </div>
          )}
        </div>

        {!sonuc && <div className="yapim"><Search size={36} /><p>Seçimleri yapıp Listele'ye basın.</p></div>}

        {sonuc && (
          <div className="tablo-kap yazdirilacak">
            <div className="yazdir-baslik">{sonuc.baslik}{sonuc.ozet ? ` · ${sonuc.ozet}` : ''}</div>
            <table className="tablo">
              <thead>
                <tr>
                  {sonuc.kolonlar.map(k => (
                    <th key={k.ad} className="siralanabilir" onClick={() => setSiralama(s => s?.ad === k.ad ? { ad: k.ad, artan: !s.artan } : { ad: k.ad, artan: true })}>
                      {k.baslik}
                      {siralama?.ad === k.ad && (siralama.artan ? <ArrowUp size={12} /> : <ArrowDown size={12} />)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {satirlar.map((s, i) => (
                  <tr key={i}>
                    {sonuc.kolonlar.map(k => (
                      <td key={k.ad} className={['sayi', 'sure', 'km', 'tarih'].includes(k.tur) ? 'sayi-hucre' : ''}>
                        {k.tur === 'koordinat' && s[k.ad]
                          ? <a className="harita-baglanti" href={`https://www.google.com/maps?q=${s[k.ad]}`} target="_blank" rel="noreferrer"><MapPin size={13} />Haritada</a>
                          : hucreMetni(s[k.ad], k)}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
            {satirlar.length === 0 && <p className="tablo-not">Kayıt bulunamadı.</p>}
          </div>
        )}
      </section>
    </div>
  )
}
