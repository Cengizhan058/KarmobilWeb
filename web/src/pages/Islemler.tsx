import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import L from 'leaflet'
import { Circle, MapContainer, Marker, Polygon, Polyline, TileLayer, Tooltip, ZoomControl, useMap, useMapEvents } from 'react-leaflet'
import { Layers, MapPin, Pentagon, Plus, Route, Save, Trash2, Undo2, Eraser, Share2, X } from 'lucide-react'
import { api, ApiHatasi, type HaritaKatmanlari } from '../api/client.ts'
import { poiIkonu } from '../lib/poi.ts'

type Sekme = 'nokta' | 'bolge' | 'rota'
type Konum = [number, number]
type Katman = HaritaKatmanlari['katmanlar'][number]

/** Formda düzenlenen kayıt; id null ise yeni kayıt. */
type Duzen =
  | { tur: 'katman'; id: number | null; ad: string; ikon: string; boyut: number; paylas: boolean }
  | { tur: 'nokta'; katmanId: number; id: number | null; baslik: string; cap: number; konum: Konum | null }
  | { tur: 'bolge' | 'rota'; id: number | null; ad: string; renk: string; noktalar: Konum[]; ciz: boolean }

// Eski sitedeki ikon listesi (images/poi-ico/poiN.png); 5 ve 12 eski listede de yoktu.
const IKONLAR: [string, string][] = [
  ['0', 'Standart'], ['1', 'Daire'], ['2', 'Dörtgen'], ['3', 'Banka-Postane'], ['4', 'Benzin İstasyonu'], ['6', 'Ev'],
  ['7', 'Fabrika'], ['8', 'Hastane'], ['9', 'Havaalanı'], ['10', 'Ofis'], ['11', 'Okul'], ['13', 'Şirket'], ['14', 'Stadyum'],
  ['15', 'Happy'], ...Array.from({ length: 18 }, (_, i): [string, string] => [String(16 + i), `Pin ${i + 1}`]),
]
const BOYUTLAR = ['Çok küçük', 'Küçük', 'Orta', 'Büyük', 'Çok büyük']

const isaretIkonu = L.divIcon({ className: 'islem-isaret', iconSize: [14, 14] })

export default function Islemler() {
  const [params, setParams] = useSearchParams()
  const sekme = (['nokta', 'bolge', 'rota'].includes(params.get('sekme') ?? '') ? params.get('sekme') : 'nokta') as Sekme
  const [veri, setVeri] = useState<HaritaKatmanlari | null>(null)
  const [katmanId, setKatmanId] = useState<number | null>(null)
  const [duzen, setDuzen] = useState<Duzen | null>(null)
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  const yukle = () => api.haritaKatmanlari().then(setVeri).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  useEffect(() => { yukle() }, [])

  const yeniBos = (s: Sekme): Duzen | null =>
    s === 'nokta' ? { tur: 'katman', id: null, ad: '', ikon: '16', boyut: 2, paylas: false }
      : { tur: s, id: null, ad: '', renk: s === 'bolge' ? '#e8590c' : '#1c7ed6', noktalar: [], ciz: true }

  // Menüden "Yeni ..." ile gelindiyse form boş açılır; sekme değişince seçim sıfırlanır.
  useEffect(() => {
    setDuzen(params.get('yeni') && sekme !== 'nokta' ? yeniBos(sekme) : null)
    setKatmanId(null)
    setMesaj(null)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sekme, params.get('yeni')])

  const sekmeSec = (s: Sekme) => setParams({ sekme: s })
  const katman = veri?.katmanlar.find(k => k.id === katmanId) ?? null

  const calistir = async (is_: () => Promise<{ id?: number; mesaj: string }>, sonra?: (id?: number) => void) => {
    setMesgul(true); setMesaj(null)
    try {
      const r = await is_()
      setMesaj({ tur: 'ok', metin: r.mesaj })
      await yukle()
      sonra?.(r.id)
    } catch (e) {
      setMesaj({ tur: 'hata', metin: e instanceof ApiHatasi || e instanceof Error ? e.message : 'İşlem başarısız.' })
    } finally {
      setMesgul(false)
    }
  }

  const kaydet = (e: FormEvent) => {
    e.preventDefault()
    if (!duzen) return
    if (duzen.tur === 'katman') {
      const { id, ad, ikon, boyut, paylas } = duzen
      calistir(() => api.katmanKaydet(id, { ad, ikon, boyut, paylas }), yeniId => { if (!id && yeniId) setKatmanId(yeniId); setDuzen(null) })
    } else if (duzen.tur === 'nokta') {
      if (!duzen.konum) { setMesaj({ tur: 'hata', metin: 'Haritaya tıklayarak noktanın yerini seçiniz.' }); return }
      const { katmanId: kid, id, baslik, cap, konum } = duzen
      calistir(() => api.noktaKaydet(kid, id, { baslik, cap, lat: konum[0], lon: konum[1] }), () => setDuzen(null))
    } else {
      const { tur, id, ad, renk, noktalar, ciz } = duzen
      const yol = tur === 'bolge' ? 'bolgeler' : 'rotalar'
      // Rotanın şekli eski sitede de sonradan değiştirilemiyordu; yalnız ad/renk güncellenir.
      const sekil = !id || (ciz && tur === 'bolge') ? noktalar : null
      calistir(() => api.sekilKaydet(yol, id, { ad, renk, noktalar: sekil }), () => setDuzen(null))
    }
  }

  const sil = () => {
    if (!duzen || duzen.id == null) return
    const ad = duzen.tur === 'nokta' ? duzen.baslik : duzen.ad
    const ek = duzen.tur === 'katman' ? ' Gruptaki tüm noktalar da silinir.' : ''
    if (!confirm(`"${ad}" silinsin mi?${ek}`)) return
    const id = duzen.id
    calistir(
      () => duzen.tur === 'katman' ? api.katmanSil(id) : duzen.tur === 'nokta' ? api.noktaSil(id) : api.sekilSil(duzen.tur === 'bolge' ? 'bolgeler' : 'rotalar', id),
      () => { if (duzen.tur === 'katman') setKatmanId(null); setDuzen(null) },
    )
  }

  const haritaTik = (k: Konum) => {
    setDuzen(d => {
      if (!d) return d
      if (d.tur === 'nokta') return { ...d, konum: k }
      if ((d.tur === 'bolge' || d.tur === 'rota') && d.ciz) return { ...d, noktalar: [...d.noktalar, k] }
      return d
    })
  }

  const katmanSec = (k: Katman) => {
    setKatmanId(k.id)
    setDuzen(k.paylasilan ? null : { tur: 'katman', id: k.id, ad: k.ad, ikon: k.ikon ?? '0', boyut: k.boyut || 2, paylas: k.paylas })
  }

  // Seçilen kaydın sınırlarına yaklaşılır.
  const odak = useMemo<Konum[] | null>(() => {
    if (duzen?.tur === 'nokta') return duzen.konum ? [duzen.konum] : null
    if ((duzen?.tur === 'bolge' || duzen?.tur === 'rota') && duzen.id) return duzen.noktalar
    if (katman && katman.noktalar.length) return katman.noktalar.map(n => [n.lat, n.lon])
    // Seçim yoksa sekmedeki tüm kayıtlar sığdırılır.
    if (!veri || duzen) return null
    const tumu: Konum[] = sekme === 'nokta' ? veri.katmanlar.flatMap(k => k.noktalar.map((n): Konum => [n.lat, n.lon]))
      : (sekme === 'bolge' ? veri.bolgeler : veri.rotalar).flatMap(s => s.noktalar)
    return tumu.length ? tumu : null
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [veri === null, sekme, duzen?.tur, duzen?.id, katmanId])

  const ciziliyor = (duzen?.tur === 'bolge' || duzen?.tur === 'rota') && duzen.ciz

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form islem-panel">
        <nav className="bilgi-sekmeler islem-sekmeler">
          <button className={sekme === 'nokta' ? 'secili' : ''} onClick={() => sekmeSec('nokta')}><MapPin size={15} />Noktalar</button>
          <button className={sekme === 'bolge' ? 'secili' : ''} onClick={() => sekmeSec('bolge')}><Pentagon size={15} />Bölgeler</button>
          <button className={sekme === 'rota' ? 'secili' : ''} onClick={() => sekmeSec('rota')}><Route size={15} />Rotalar</button>
        </nav>

        {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}

        {/* ---------- Liste ---------- */}
        {sekme === 'nokta' && (
          <>
            <div className="islem-baslik">
              <h3>Nokta grupları</h3>
              <button className="ikincil-dugme" onClick={() => { setKatmanId(null); setDuzen(yeniBos('nokta')) }}><Plus size={15} />Yeni grup</button>
            </div>
            <ul className="islem-liste">
              {veri?.katmanlar.map(k => (
                <li key={k.id} className={k.id === katmanId ? 'secili' : ''} onClick={() => katmanSec(k)}>
                  <img src={`/poi-ico/poi${k.ikon ?? 0}.png`} alt="" />
                  <span>{k.ad}</span>
                  {k.paylasilan && <Share2 size={13} className="soluk" aria-label="Paylaşılan" />}
                  <small>{k.noktalar.length}</small>
                </li>
              ))}
              {veri && !veri.katmanlar.length && <li className="bos">Henüz nokta grubu yok.</li>}
            </ul>

            {katman && (
              <>
                <div className="islem-baslik">
                  <h3>{katman.ad} noktaları</h3>
                  {!katman.paylasilan && (
                    <button className="ikincil-dugme" onClick={() => setDuzen({ tur: 'nokta', katmanId: katman.id, id: null, baslik: '', cap: 100, konum: null })}>
                      <Plus size={15} />Yeni nokta
                    </button>
                  )}
                </div>
                {katman.paylasilan && <p className="soluk kucuk-not">Bu grup ana hesap tarafından paylaşıldı; değiştirilemez.</p>}
                <ul className="islem-liste">
                  {katman.noktalar.map(n => (
                    <li key={n.id} className={duzen?.tur === 'nokta' && duzen.id === n.id ? 'secili' : ''}
                      onClick={() => !katman.paylasilan && setDuzen({ tur: 'nokta', katmanId: katman.id, id: n.id, baslik: n.baslik, cap: n.cap ?? 100, konum: [n.lat, n.lon] })}>
                      <MapPin size={14} className="soluk" /><span>{n.baslik}</span>{n.cap ? <small>{n.cap} m</small> : null}
                    </li>
                  ))}
                  {!katman.noktalar.length && <li className="bos">Bu grupta nokta yok.</li>}
                </ul>
              </>
            )}
          </>
        )}

        {(sekme === 'bolge' || sekme === 'rota') && (
          <>
            <div className="islem-baslik">
              <h3>{sekme === 'bolge' ? 'Bölgeler' : 'Rotalar'}</h3>
              <button className="ikincil-dugme" onClick={() => setDuzen(yeniBos(sekme))}><Plus size={15} />{sekme === 'bolge' ? 'Yeni bölge' : 'Yeni rota'}</button>
            </div>
            <ul className="islem-liste">
              {(sekme === 'bolge' ? veri?.bolgeler : veri?.rotalar)?.map(s => (
                <li key={s.id} className={duzen?.tur === sekme && duzen.id === s.id ? 'secili' : ''}
                  onClick={() => setDuzen({ tur: sekme, id: s.id, ad: s.ad, renk: s.renk, noktalar: s.noktalar, ciz: false })}>
                  <i className="renk-nokta" style={{ background: s.renk }} /><span>{s.ad}</span>
                </li>
              ))}
              {veri && !(sekme === 'bolge' ? veri.bolgeler : veri.rotalar).length && <li className="bos">Kayıt yok.</li>}
            </ul>
          </>
        )}

        {/* ---------- Form ---------- */}
        {duzen && (
          <form className="islem-form" onSubmit={kaydet}>
            <div className="islem-baslik">
              <h3>{baslik(duzen)}</h3>
              <button type="button" className="ikon-dugme" title="Kapat" onClick={() => setDuzen(null)}><X size={16} /></button>
            </div>

            {duzen.tur === 'katman' && (
              <>
                <label className="alan"><span>Grup adı</span>
                  <input className="secim" value={duzen.ad} maxLength={100} onChange={e => setDuzen({ ...duzen, ad: e.target.value })} required autoFocus />
                </label>
                <div className="alan"><span>İkon</span>
                  <div className="ikon-izgara">
                    {IKONLAR.map(([d, ad]) => (
                      <button type="button" key={d} title={ad} className={duzen.ikon === d ? 'secili' : ''} onClick={() => setDuzen({ ...duzen, ikon: d })}>
                        <img src={`/poi-ico/poi${d}.png`} alt={ad} />
                      </button>
                    ))}
                  </div>
                </div>
                <label className="alan"><span>Boyut</span>
                  <select className="secim" value={duzen.boyut} onChange={e => setDuzen({ ...duzen, boyut: Number(e.target.value) })}>
                    {BOYUTLAR.map((b, i) => <option key={b} value={i + 1}>x{i + 1} ({b})</option>)}
                  </select>
                </label>
                <label className="onay-satir"><input type="checkbox" checked={duzen.paylas} onChange={e => setDuzen({ ...duzen, paylas: e.target.checked })} />Alt kullanıcılarla paylaş</label>
              </>
            )}

            {duzen.tur === 'nokta' && (
              <>
                <label className="alan"><span>Nokta adı</span>
                  <input className="secim" value={duzen.baslik} maxLength={100} onChange={e => setDuzen({ ...duzen, baslik: e.target.value })} required autoFocus />
                </label>
                <label className="alan"><span>Çap: {duzen.cap} metre</span>
                  <input type="range" min={10} max={2000} step={10} value={duzen.cap} onChange={e => setDuzen({ ...duzen, cap: Number(e.target.value) })} />
                </label>
                <p className="kucuk-not">
                  {duzen.konum ? `Konum: ${duzen.konum[0].toFixed(6)}, ${duzen.konum[1].toFixed(6)} — değiştirmek için haritaya tıklayın ya da işareti sürükleyin.`
                    : 'Noktanın yerini seçmek için haritaya tıklayın.'}
                </p>
              </>
            )}

            {(duzen.tur === 'bolge' || duzen.tur === 'rota') && (
              <>
                <label className="alan"><span>{duzen.tur === 'bolge' ? 'Bölge adı' : 'Rota adı'}</span>
                  <input className="secim" value={duzen.ad} maxLength={100} onChange={e => setDuzen({ ...duzen, ad: e.target.value })} required autoFocus />
                </label>
                <label className="alan renk-alan"><span>Renk</span>
                  <input type="color" value={duzen.renk} onChange={e => setDuzen({ ...duzen, renk: e.target.value })} />
                </label>
                {duzen.ciz ? (
                  <>
                    <p className="kucuk-not">
                      Haritaya tıklayarak {duzen.tur === 'bolge' ? 'bölgenin köşelerini (en az 3)' : 'rotanın noktalarını sırayla (en az 2)'} işaretleyin. {duzen.noktalar.length} nokta.
                    </p>
                    <div className="gun-dugmeleri">
                      <button type="button" className="ikincil-dugme" disabled={!duzen.noktalar.length} onClick={() => setDuzen({ ...duzen, noktalar: duzen.noktalar.slice(0, -1) })}><Undo2 size={15} />Geri al</button>
                      <button type="button" className="ikincil-dugme" disabled={!duzen.noktalar.length} onClick={() => setDuzen({ ...duzen, noktalar: [] })}><Eraser size={15} />Temizle</button>
                    </div>
                  </>
                ) : duzen.tur === 'bolge' ? (
                  <button type="button" className="ikincil-dugme tam" onClick={() => setDuzen({ ...duzen, ciz: true, noktalar: [] })}><Pentagon size={15} />Şekli yeniden çiz</button>
                ) : null}
              </>
            )}

            <div className="islem-dugmeler">
              <button className="ana-dugme" type="submit" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={16} />Kaydet</>}</button>
              {duzen.id != null && <button type="button" className="ikincil-dugme tehlike" disabled={mesgul} onClick={sil}><Trash2 size={15} />Sil</button>}
            </div>
          </form>
        )}

        {!duzen && !katman && (
          <div className="yapim kucuk">
            {sekme === 'nokta' ? <Layers size={30} /> : sekme === 'bolge' ? <Pentagon size={30} /> : <Route size={30} />}
            <p>Düzenlemek için listeden bir kayıt seçin ya da yenisini ekleyin.</p>
          </div>
        )}
      </aside>

      <section className={`gecmis-icerik${ciziliyor || duzen?.tur === 'nokta' ? ' cizim-modu' : ''}`}>
        <div className="gecmis-harita">
        <MapContainer className="harita" center={[39, 35]} zoom={6} zoomControl={false}>
          <ZoomControl position="bottomright" />
          <TileLayer url="https://{s}.google.com/vt/hl=tr&x={x}&y={y}&z={z}" subdomains={['mts0', 'mts1', 'mts2', 'mts3']} maxZoom={20} attribution="Google" />
          <TikDinleyici onTik={haritaTik} />
          <Odaklan noktalar={odak} />

          {sekme === 'nokta' && veri?.katmanlar.filter(k => !katmanId || k.id === katmanId).flatMap(k => k.noktalar
            .filter(n => !(duzen?.tur === 'nokta' && duzen.id === n.id))
            .map(n => (
              <Marker key={n.id} position={[n.lat, n.lon]} icon={poiIkonu(k.ikon, k.boyut)}>
                <Tooltip direction="top">{n.baslik}<br /><span className="soluk">{k.ad}</span></Tooltip>
              </Marker>
            )))}
          {duzen?.tur === 'nokta' && duzen.konum && (
            <>
              <Circle center={duzen.konum} radius={duzen.cap} pathOptions={{ color: '#1c7ed6', weight: 1, fillOpacity: .12 }} />
              <Marker position={duzen.konum} draggable icon={poiIkonu(katman?.ikon ?? null, katman?.boyut ?? 2)}
                eventHandlers={{ dragend: e => { const p = (e.target as L.Marker).getLatLng(); setDuzen({ ...duzen, konum: [p.lat, p.lng] }) } }} />
            </>
          )}

          {sekme === 'bolge' && veri?.bolgeler.filter(b => !(duzen?.tur === 'bolge' && duzen.id === b.id)).map(b => (
            <Polygon key={b.id} positions={b.noktalar} pathOptions={{ color: b.renk, weight: 1, fillOpacity: .15 }}
              eventHandlers={{ click: () => !ciziliyor && setDuzen({ tur: 'bolge', id: b.id, ad: b.ad, renk: b.renk, noktalar: b.noktalar, ciz: false }) }}>
              <Tooltip sticky>{b.ad}</Tooltip>
            </Polygon>
          ))}
          {sekme === 'rota' && veri?.rotalar.filter(r => !(duzen?.tur === 'rota' && duzen.id === r.id)).map(r => (
            <Polyline key={r.id} positions={r.noktalar} pathOptions={{ color: r.renk, weight: 4, opacity: .6 }}
              eventHandlers={{ click: () => !ciziliyor && setDuzen({ tur: 'rota', id: r.id, ad: r.ad, renk: r.renk, noktalar: r.noktalar, ciz: false }) }}>
              <Tooltip sticky>{r.ad}</Tooltip>
            </Polyline>
          ))}

          {duzen?.tur === 'bolge' && duzen.noktalar.length > 0 && (
            <Polygon positions={duzen.noktalar} pathOptions={{ color: duzen.renk, weight: 3, fillOpacity: .3, dashArray: duzen.ciz ? '6 4' : undefined }} />
          )}
          {duzen?.tur === 'rota' && duzen.noktalar.length > 0 && (
            <Polyline positions={duzen.noktalar} pathOptions={{ color: duzen.renk, weight: 5, opacity: .9 }} />
          )}
          {ciziliyor && duzen.noktalar.map((n, i) => <Marker key={i} position={n} icon={isaretIkonu} interactive={false} />)}
        </MapContainer>
        </div>
      </section>
    </div>
  )
}

function baslik(d: Duzen) {
  const yeni = d.id == null
  switch (d.tur) {
    case 'katman': return yeni ? 'Yeni nokta grubu' : 'Grubu düzenle'
    case 'nokta': return yeni ? 'Yeni nokta' : 'Noktayı düzenle'
    case 'bolge': return yeni ? 'Yeni bölge' : 'Bölgeyi düzenle'
    case 'rota': return yeni ? 'Yeni rota' : 'Rotayı düzenle'
  }
}

function TikDinleyici({ onTik }: { onTik: (k: Konum) => void }) {
  useMapEvents({ click: e => onTik([e.latlng.lat, e.latlng.lng]) })
  return null
}

function Odaklan({ noktalar }: { noktalar: Konum[] | null }) {
  const map = useMap()
  useEffect(() => {
    if (!noktalar?.length) return
    if (noktalar.length === 1) map.setView(noktalar[0], Math.max(map.getZoom(), 15))
    else map.fitBounds(L.latLngBounds(noktalar), { padding: [40, 40], maxZoom: 17 })
  }, [map, noktalar])
  return null
}
