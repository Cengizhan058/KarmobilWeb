import { useState } from 'react'
import { MapContainer, TileLayer } from 'react-leaflet'
import type { LatLngBoundsExpression } from 'leaflet'
import { useKullanici } from '../components/Duzen.tsx'

// Eski sitedeki Google katmanlarıyla aynı (index.asp tMapInit).
const KATMANLAR = {
  sokak: { ad: 'Sokak', url: 'https://{s}.google.com/vt/hl=tr&x={x}&y={y}&z={z}', sub: ['mts0', 'mts1', 'mts2', 'mts3'] },
  uydu: { ad: 'Uydu', url: 'https://{s}.google.com/vt/lyrs=y&x={x}&y={y}&z={z}', sub: ['mt0', 'mt1', 'mt2', 'mt3'] },
} as const

type KatmanId = keyof typeof KATMANLAR

// Türkiye geneli; kullanıcının users.MAPX/MAPY sınırı varsa o kullanılır.
const VARSAYILAN_SINIR: LatLngBoundsExpression = [[35.8, 25.6], [42.2, 44.9]]

export default function Harita() {
  const k = useKullanici()
  const [katman, setKatman] = useState<KatmanId>('sokak')

  const sinir: LatLngBoundsExpression = k.haritaSiniri
    ? [[k.haritaSiniri.y1, k.haritaSiniri.x1], [k.haritaSiniri.y2, k.haritaSiniri.x2]]
    : VARSAYILAN_SINIR

  const t = KATMANLAR[katman]

  return (
    <div className="harita-sayfa">
      <aside className="arac-paneli">
        <div className="panel-baslik">Araçlar</div>
        <div className="panel-bos">Araç listesi 2. aşamada eklenecek.</div>
      </aside>

      <div className="harita-alan">
        <MapContainer bounds={sinir} className="harita" zoomControl={true}>
          <TileLayer key={katman} url={t.url} subdomains={[...t.sub]} maxZoom={20} attribution="Google" />
        </MapContainer>
        <div className="katman-secici">
          {(Object.keys(KATMANLAR) as KatmanId[]).map(id => (
            <button key={id} className={id === katman ? 'secili' : ''} onClick={() => setKatman(id)}>
              {KATMANLAR[id].ad}
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
