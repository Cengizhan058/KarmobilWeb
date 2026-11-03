import { useEffect, useMemo, useRef } from 'react'
import { MapContainer, Marker, Polyline, TileLayer, Tooltip, ZoomControl, useMap } from 'react-leaflet'
import L from 'leaflet'
import type { Duraklama, GecmisNokta } from '../api/client.ts'
import { sure } from '../lib/arac.ts'

const saat = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' })

// Hız bandına göre rota rengi.
// Bu süreden uzun kayıt gelmeyen ve araç bu mesafeden fazla yer değiştiren aralık "veri yok" olarak kesikli çizilir.
const BOSLUK_SANIYE = 120
const BOSLUK_METRE = 300
const VERI_YOK_RENGI = '#8a919e'

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
  // Aynı renkteki ardışık noktalar tek çizgi olsun (binlerce küçük çizgi yerine). Sapma işaretli kayıtlar çizilmez;
  // uzun veri boşlukları ayrı, kesikli gri parça olur (araç o arada gerçekte yolu izledi ama kayıt yok).
  const cizilen = useMemo(() => noktalar.filter(n => !n.sapma), [noktalar])
  const parcalar = useMemo(() => {
    const sonuc: { renk: string; kesik: boolean; yol: [number, number][] }[] = []
    for (let i = 1; i < cizilen.length; i++) {
      const p = cizilen[i - 1], q = cizilen[i]
      const a: [number, number] = [p.lat, p.lon]
      const b: [number, number] = [q.lat, q.lon]
      const kesik = (new Date(q.zaman).getTime() - new Date(p.zaman).getTime()) / 1000 > BOSLUK_SANIYE
        && L.latLng(a).distanceTo(b) > BOSLUK_METRE
      const renk = kesik ? VERI_YOK_RENGI : hizRengi(q.hiz)
      const son = sonuc[sonuc.length - 1]
      if (son && son.renk === renk && son.kesik === kesik && !kesik) son.yol.push(b)
      else sonuc.push({ renk, kesik, yol: [a, b] })
    }
    return sonuc
  }, [cizilen])

  const gorunenDuraklar = duraklar.filter(d => d.dakika >= durakEsikDakika)
  const secili = seciliIndeks != null ? noktalar[seciliIndeks] : null
  const ilk = noktalar[0]
  const son = noktalar[noktalar.length - 1]

  return (
    <MapContainer className="harita" center={[39, 35]} zoom={6} zoomControl={false}>
      <ZoomControl position="bottomright" />
      <BoyutYenile />
      <TileLayer url="https://{s}.google.com/vt/hl=tr&x={x}&y={y}&z={z}" subdomains={['mts0', 'mts1', 'mts2', 'mts3']} maxZoom={20} attribution="Google" />
      <Sigdir noktalar={cizilen} />
      <SeciliyeKaydir nokta={secili} takip={kaydirIstegi} />

      {parcalar.map((p, i) => (
        <Polyline key={i} positions={p.yol} pathOptions={p.kesik
          ? { color: p.renk, weight: 3, opacity: .9, dashArray: '6 8' }
          : { color: p.renk, weight: 5, opacity: .9 }}>
          {p.kesik && <Tooltip sticky>Bu aralıkta cihazdan kayıt gelmedi</Tooltip>}
        </Polyline>
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
