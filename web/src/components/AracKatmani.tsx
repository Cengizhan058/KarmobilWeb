import { useEffect, useMemo, useState } from 'react'
import { Marker, useMap } from 'react-leaflet'
import L from 'leaflet'
import Supercluster from 'supercluster'
import type { AracKonum } from '../api/client.ts'
import { DURUMLAR } from '../lib/arac.ts'

type Props = {
  araclar: AracKonum[]
  grupla: boolean
  /** 0 plaka, 1 cihaz ID, 2 sürücü, 3 hız (users.GORUNUM) */
  etiket: number
  seciliId: number | null
  onSec: (a: AracKonum) => void
}

const kacis = (s: string) => s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!))

function etiketMetni(a: AracKonum, etiket: number): string {
  switch (etiket) {
    case 1: return a.cihazId
    case 2: return a.surucu ?? a.plaka
    case 3: return `${Math.round(a.hiz)} km/s`
    default: return a.plaka
  }
}

function aracIkonu(a: AracKonum, secili: boolean, etiket: number) {
  const renk = DURUMLAR[a.durum].renk
  // Hareketli araçta yön oku, diğerlerinde nokta.
  const isaret = a.durum === 'Hareketli'
    ? `<svg viewBox="0 0 24 24" width="16" height="16" style="transform:rotate(${a.yon}deg)"><path d="M12 2 L20 21 L12 16 L4 21 Z" fill="#fff"/></svg>`
    : '<span class="ic-nokta"></span>'
  return L.divIcon({
    className: 'arac-isaret' + (secili ? ' secili' : ''),
    html: `<div class="isaret-daire" style="background:${renk}">${isaret}</div><div class="isaret-plaka">${kacis(etiketMetni(a, etiket))}</div>`,
    iconSize: [0, 0],
    iconAnchor: [0, 0],
  })
}

function kumeIkonu(sayi: number) {
  const boyut = sayi < 10 ? 36 : sayi < 50 ? 44 : 52
  return L.divIcon({
    className: 'kume-isaret',
    html: `<div style="width:${boyut}px;height:${boyut}px;margin:-${boyut / 2}px 0 0 -${boyut / 2}px">${sayi}</div>`,
    iconSize: [0, 0],
  })
}

type Nokta = Supercluster.PointFeature<{ id: number }>

export default function AracKatmani({ araclar, grupla, etiket, seciliId, onSec }: Props) {
  const map = useMap()
  const [gorus, setGorus] = useState(0)

  // Harita kaydırılınca/yakınlaşınca kümeler yeniden hesaplansın.
  useEffect(() => {
    const yenile = () => setGorus(n => n + 1)
    map.on('moveend', yenile)
    return () => { map.off('moveend', yenile) }
  }, [map])

  const tekTek = (list: AracKonum[]) => list.map(a => (
    <Marker
      key={a.id}
      position={[a.lat!, a.lon!]}
      icon={aracIkonu(a, a.id === seciliId, etiket)}
      zIndexOffset={a.id === seciliId ? 1000 : 0}
      eventHandlers={{ click: () => onSec(a) }}
    />
  ))

  // Seçili araç kümeye girmez; her zaman ayrı ve vurgulu çizilir.
  const kumelenecek = useMemo(() => araclar.filter(a => a.id !== seciliId), [araclar, seciliId])
  const indeks = useMemo(() => {
    if (!grupla) return null
    const s = new Supercluster<{ id: number }>({ radius: 60, maxZoom: 16 })
    s.load(kumelenecek.map<Nokta>(a => ({
      type: 'Feature',
      properties: { id: a.id },
      geometry: { type: 'Point', coordinates: [a.lon!, a.lat!] },
    })))
    return s
  }, [grupla, kumelenecek])

  if (!indeks) return <>{tekTek(araclar)}</>

  void gorus
  const b = map.getBounds()
  const parcalar = indeks.getClusters([b.getWest(), b.getSouth(), b.getEast(), b.getNorth()], Math.round(map.getZoom()))
  const aracMap = new Map(kumelenecek.map(a => [a.id, a]))
  const secili = araclar.find(a => a.id === seciliId)

  return (
    <>
      {parcalar.map(p => {
        const [lon, lat] = p.geometry.coordinates
        if ('cluster' in p.properties && p.properties.cluster) {
          const kumeId = p.properties.cluster_id as number
          const sayi = p.properties.point_count as number
          return (
            <Marker
              key={`k${kumeId}`}
              position={[lat, lon]}
              icon={kumeIkonu(sayi)}
              eventHandlers={{ click: () => map.flyTo([lat, lon], Math.min(indeks.getClusterExpansionZoom(kumeId), 18), { duration: .5 }) }}
            />
          )
        }
        const a = aracMap.get((p.properties as { id: number }).id)
        return a ? tekTek([a]) : null
      })}
      {secili && tekTek([secili])}
    </>
  )
}
