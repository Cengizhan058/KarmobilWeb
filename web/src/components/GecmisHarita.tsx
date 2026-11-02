import { useEffect, useMemo, useRef } from 'react'
import { MapContainer, Marker, Polyline, TileLayer, Tooltip, ZoomControl, useMap } from 'react-leaflet'
import L from 'leaflet'
import type { Duraklama, GecmisNokta } from '../api/client.ts'
import { sure } from '../lib/arac.ts'

const saat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' })

// Hız bandına göre rota rengi.
const hizRengi = (h: number) => (h >= 90 ? '#d9534f' : h >= 50 ? '#e0a526' : '#2fa36b')

const kucukIkon = (sinif: string, yazi: string) =>
  L.divIcon({ className: 'gecmis-isaret ' + sinif, html: `<div>${yazi}</div>`, iconSize: [0, 0] })

const aracIkonu = (yon: number, hiz: number) => L.divIcon({
  className: 'arac-isaret secili',
  html: `<div class="isaret-daire" style="background:${hiz > 0 ? '#2b8fd6' : '#d9534f'}">${hiz > 0
    ? `<svg viewBox="0 0 24 24" width="16" height="16" style="transform:rotate(${yon}deg)"><path d="M12 2 L20 21 L12 16 L4 21 Z" fill="#fff"/></svg>`
    : '<span class="ic-nokta"></span>'}</div>`,
  iconSize: [0, 0],
})

function Sigdir({ noktalar }: { noktalar: GecmisNokta[] }) {
  const map = useMap()
  useEffect(() => {
    if (noktalar.length === 0) return
    if (noktalar.length === 1) map.setView([noktalar[0].lat, noktalar[0].lon], 15)
    else map.fitBounds(noktalar.map(n => [n.lat, n.lon] as [number, number]), { padding: [40, 40] })
  }, [noktalar, map])
  return null
}

function SeciliyeKaydir({ nokta, takip }: { nokta: GecmisNokta | null; takip: number }) {
  const map = useMap()
  const son = useRef(0)
  useEffect(() => {
    if (!nokta || takip === son.current) return
    son.current = takip
    if (!map.getBounds().pad(-0.15).contains([nokta.lat, nokta.lon])) map.panTo([nokta.lat, nokta.lon])
  }, [nokta, takip, map])
  return null
}

// Kap boyutu değişince (sekme açılınca) Leaflet'e bildir.
function BoyutYenile() {
  const map = useMap()
  useEffect(() => {
    const t = setTimeout(() => map.invalidateSize(), 150)
    return () => clearTimeout(t)
  }, [map])
  return null
}

type Props = {
  noktalar: GecmisNokta[]
  duraklar: Duraklama[]
  durakEsikDakika: number
  seciliIndeks: number | null
  /** Liste/grafikten seçim yapıldığında artar; harita gerekirse kaydırılır. */
  kaydirIstegi: number
}

export default function GecmisHarita({ noktalar, duraklar, durakEsikDakika, seciliIndeks, kaydirIstegi }: Props) {
  // Aynı renkteki ardışık noktalar tek çizgi olsun (binlerce küçük çizgi yerine).
  const parcalar = useMemo(() => {
    const sonuc: { renk: string; yol: [number, number][] }[] = []
    for (let i = 1; i < noktalar.length; i++) {
      const renk = hizRengi(noktalar[i].hiz)
      const a: [number, number] = [noktalar[i - 1].lat, noktalar[i - 1].lon]
      const b: [number, number] = [noktalar[i].lat, noktalar[i].lon]
      const son = sonuc[sonuc.length - 1]
      if (son && son.renk === renk) son.yol.push(b)
      else sonuc.push({ renk, yol: [a, b] })
    }
    return sonuc
  }, [noktalar])

  const gorunenDuraklar = duraklar.filter(d => d.dakika >= durakEsikDakika)
  const secili = seciliIndeks != null ? noktalar[seciliIndeks] : null
  const ilk = noktalar[0]
  const son = noktalar[noktalar.length - 1]

  return (
    <MapContainer className="harita" center={[39, 35]} zoom={6} zoomControl={false}>
      <ZoomControl position="bottomright" />
      <BoyutYenile />
      <TileLayer url="https://{s}.google.com/vt/hl=tr&x={x}&y={y}&z={z}" subdomains={['mts0', 'mts1', 'mts2', 'mts3']} maxZoom={20} attribution="Google" />
      <Sigdir noktalar={noktalar} />
      <SeciliyeKaydir nokta={secili} takip={kaydirIstegi} />

      {parcalar.map((p, i) => (
        <Polyline key={i} positions={p.yol} pathOptions={{ color: p.renk, weight: 5, opacity: .9 }} />
      ))}

      {gorunenDuraklar.map(d => (
        <Marker key={d.baslangic} position={[d.lat, d.lon]} icon={kucukIkon('durak', 'P')}>
          <Tooltip direction="top" offset={[0, -12]}>
            <strong>{sure(d.dakika)} durdu</strong><br />
            {saat.format(new Date(d.baslangic))} – {saat.format(new Date(d.bitis))}<br />
            {d.adres ?? ''}
          </Tooltip>
        </Marker>
      ))}

      {ilk && <Marker position={[ilk.lat, ilk.lon]} icon={kucukIkon('baslangic', 'B')}><Tooltip>Başlangıç · {saat.format(new Date(ilk.zaman))}</Tooltip></Marker>}
      {son && son !== ilk && <Marker position={[son.lat, son.lon]} icon={kucukIkon('bitis', 'S')}><Tooltip>Son · {saat.format(new Date(son.zaman))}</Tooltip></Marker>}

      {secili && <Marker position={[secili.lat, secili.lon]} icon={aracIkonu(secili.yon, secili.hiz)} zIndexOffset={2000} />}
    </MapContainer>
  )
}
