import L from 'leaflet'

/** Nokta grubu ikonu (eski images/poi-ico/poiN.png); boyut 1-5 → 16-80 px, eski sitedeki gibi. */
export const poiIkonu = (ikon: string | null, boyut: number) => {
  const p = 16 * Math.min(Math.max(boyut || 2, 1), 5)
  return L.icon({ iconUrl: `/poi-ico/poi${ikon ?? 0}.png`, iconSize: [p, p], iconAnchor: [p / 2, p / 2] })
}
