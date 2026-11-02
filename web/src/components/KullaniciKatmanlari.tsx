import { useEffect, useRef, useState } from 'react'
import { Circle, Marker, Polygon, Polyline, Tooltip } from 'react-leaflet'
import { Layers, X } from 'lucide-react'
import { api, type HaritaKatmanlari } from '../api/client.ts'
import { poiIkonu } from '../lib/poi.ts'

// Açık katmanlar tarayıcıda saklanır: "b:12" bölge, "k:3" nokta grubu, "r:5" rota.
const ANAHTAR = 'km-katmanlar'
const okuAcik = (): Set<string> => {
  try { return new Set(JSON.parse(localStorage.getItem(ANAHTAR) ?? '[]')) } catch { return new Set() }
}

/** Haritaya çizilen bölge / nokta / rota katmanları (MapContainer içinde kullanılır). */
export function KatmanCizimi({ veri, acik }: { veri: HaritaKatmanlari | null; acik: Set<string> }) {
  if (!veri) return null
  return (
    <>
      {veri.bolgeler.filter(b => acik.has(`b:${b.id}`)).map(b => (
        <Polygon key={`b${b.id}`} positions={b.noktalar} pathOptions={{ color: '#1f2333', weight: 2, opacity: .5, fillColor: b.renk, fillOpacity: .3 }}>
          <Tooltip sticky>{b.ad}</Tooltip>
        </Polygon>
      ))}
      {veri.rotalar.filter(r => acik.has(`r:${r.id}`)).map(r => (
        <Polyline key={`r${r.id}`} positions={r.noktalar} pathOptions={{ color: r.renk, weight: 4, opacity: .8, dashArray: '8 6' }}>
          <Tooltip sticky>{r.ad}</Tooltip>
        </Polyline>
      ))}
      {veri.katmanlar.filter(k => acik.has(`k:${k.id}`)).flatMap(k => k.noktalar.map(n => (
        <Marker key={`n${n.id}`} position={[n.lat, n.lon]} icon={poiIkonu(k.ikon, k.boyut)}>
          <Tooltip direction="top" offset={[0, -8]}><strong>{n.baslik}</strong>{n.aciklama ? <><br />{n.aciklama}</> : null}<br /><span className="soluk">{k.ad}</span></Tooltip>
        </Marker>
      )))}
      {/* POI yarıçapı (DIAMETER, metre) varsa çevresi de gösterilir. */}
      {veri.katmanlar.filter(k => acik.has(`k:${k.id}`)).flatMap(k => k.noktalar.filter(n => n.cap && n.cap > 0).map(n => (
        <Circle key={`c${n.id}`} center={[n.lat, n.lon]} radius={n.cap!} pathOptions={{ color: k.renk, weight: 1, fillOpacity: .08 }} />
      )))}
    </>
  )
}

/** Katmanları yükler ve açık/kapalı durumunu yönetir. */
export function useKullaniciKatmanlari() {
  const [veri, setVeri] = useState<HaritaKatmanlari | null>(null)
  const [acik, setAcik] = useState<Set<string>>(okuAcik)
  useEffect(() => { api.haritaKatmanlari().then(setVeri).catch(() => { /* katman yoksa düğme boş liste gösterir */ }) }, [])
  const ayarla = (y: Set<string>) => {
    setAcik(y)
    try { localStorage.setItem(ANAHTAR, JSON.stringify([...y])) } catch { /* yok say */ }
  }
  return { veri, acik, ayarla }
}

/** Harita üzerindeki "Katmanlar" düğmesi ve listesi (MapContainer dışında, harita alanının üstünde). */
export function KatmanSecici({ veri, acik, onDegis }: { veri: HaritaKatmanlari | null; acik: Set<string>; onDegis: (s: Set<string>) => void }) {
  const [gorunur, setGorunur] = useState(false)
  const kutu = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!gorunur) return
    const kapat = (e: MouseEvent) => { if (kutu.current && !kutu.current.contains(e.target as Node)) setGorunur(false) }
    document.addEventListener('mousedown', kapat)
    return () => document.removeEventListener('mousedown', kapat)
  }, [gorunur])

  const degistir = (anahtar: string, deger: boolean) => {
    const y = new Set(acik)
    if (deger) y.add(anahtar); else y.delete(anahtar)
    onDegis(y)
  }
  const grupDegistir = (anahtarlar: string[], deger: boolean) => {
    const y = new Set(acik)
    anahtarlar.forEach(a => (deger ? y.add(a) : y.delete(a)))
    onDegis(y)
  }

  const gruplar = veri ? [
    { ad: 'Bölgeler', ogeler: veri.bolgeler.map(b => ({ anahtar: `b:${b.id}`, ad: b.ad, renk: b.renk, ek: '' })) },
    { ad: 'Nokta grupları', ogeler: veri.katmanlar.map(k => ({ anahtar: `k:${k.id}`, ad: k.ad, renk: k.renk, ek: `${k.noktalar.length}` })) },
    { ad: 'Rotalar', ogeler: veri.rotalar.map(r => ({ anahtar: `r:${r.id}`, ad: r.ad, renk: r.renk, ek: '' })) },
  ] : []
  const acikSayi = [...acik].length

  return (
    <div className="katman-kutu" ref={kutu}>
      <button className="katman-dugme" onClick={() => setGorunur(g => !g)} title="Bölge, nokta ve rota katmanları">
        <Layers size={16} />Katmanlar{acikSayi > 0 && <span className="sayi">{acikSayi}</span>}
      </button>
      {gorunur && (
        <div className="katman-liste">
          <div className="katman-liste-ust"><strong>Harita katmanları</strong><button className="ikon-dugme" onClick={() => setGorunur(false)}><X size={16} /></button></div>
          {!veri && <p className="soluk">Yükleniyor…</p>}
          {gruplar.map(g => {
            const hepsi = g.ogeler.length > 0 && g.ogeler.every(o => acik.has(o.anahtar))
            return (
              <div key={g.ad} className="katman-grup">
                <label className="katman-grup-baslik">
                  <input type="checkbox" checked={hepsi} disabled={g.ogeler.length === 0} onChange={e => grupDegistir(g.ogeler.map(o => o.anahtar), e.target.checked)} />
                  {g.ad} <span className="soluk">({g.ogeler.length})</span>
                </label>
                {g.ogeler.map(o => (
                  <label key={o.anahtar} className="katman-oge">
                    <input type="checkbox" checked={acik.has(o.anahtar)} onChange={e => degistir(o.anahtar, e.target.checked)} />
                    <span className="katman-renk" style={{ background: o.renk }} />
                    <span className="katman-ad">{o.ad}</span>
                    {o.ek && <span className="soluk">{o.ek}</span>}
                  </label>
                ))}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
