import { useMemo, useRef, useState } from 'react'
import type { GecmisNokta } from '../api/client.ts'

const saat = new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit' })
const tam = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'medium' })

type Props = {
  noktalar: GecmisNokta[]
  seciliIndeks: number | null
  onSec: (i: number) => void
}

// Basamaklı hız grafiği (eski Highcharts "steps" grafiği yerine, bağımlılıksız SVG).
export default function HizGrafigi({ noktalar, seciliIndeks, onSec }: Props) {
  const kutu = useRef<SVGSVGElement>(null)
  const [ustunde, setUstunde] = useState<number | null>(null)
  const G = 1000, Y = 300, sol = 44, alt = 26, ust = 12, sag = 12

  const veri = useMemo(() => {
    if (noktalar.length < 2) return null
    const t0 = new Date(noktalar[0].zaman).getTime()
    const t1 = new Date(noktalar[noktalar.length - 1].zaman).getTime()
    const azami = Math.max(60, Math.ceil(Math.max(...noktalar.map(n => n.hiz)) / 20) * 20)
    const x = (t: number) => sol + ((t - t0) / Math.max(1, t1 - t0)) * (G - sol - sag)
    const y = (h: number) => ust + (1 - h / azami) * (Y - ust - alt)
    let yol = ''
    noktalar.forEach((n, i) => {
      const xi = x(new Date(n.zaman).getTime())
      yol += i === 0 ? `M${xi},${y(n.hiz)}` : `H${xi}V${y(n.hiz)}`
    })
    const xEtiket = Array.from({ length: 7 }, (_, i) => t0 + ((t1 - t0) * i) / 6)
    const yEtiket = Array.from({ length: azami / 20 + 1 }, (_, i) => i * 20)
    return { t0, t1, azami, x, y, yol, xEtiket, yEtiket }
  }, [noktalar])

  if (!veri) return <div className="panel-bos">Grafik için yeterli kayıt yok.</div>

  const enYakin = (clientX: number) => {
    const r = kutu.current!.getBoundingClientRect()
    const px = ((clientX - r.left) / r.width) * G
    const t = veri.t0 + ((px - sol) / (G - sol - sag)) * (veri.t1 - veri.t0)
    let iyi = 0, fark = Infinity
    noktalar.forEach((n, i) => {
      const d = Math.abs(new Date(n.zaman).getTime() - t)
      if (d < fark) { fark = d; iyi = i }
    })
    return iyi
  }

  const vurgu = ustunde ?? seciliIndeks
  const vn = vurgu != null ? noktalar[vurgu] : null

  return (
    <div className="hiz-grafik">
      <svg
        ref={kutu}
        viewBox={`0 0 ${G} ${Y}`}
        preserveAspectRatio="none"
        onMouseMove={e => setUstunde(enYakin(e.clientX))}
        onMouseLeave={() => setUstunde(null)}
        onClick={e => onSec(enYakin(e.clientX))}
      >
        {veri.yEtiket.map(h => (
          <g key={h}>
            <line x1={sol} x2={G - sag} y1={veri.y(h)} y2={veri.y(h)} className="grafik-cizgi" />
            <text x={sol - 6} y={veri.y(h) + 4} textAnchor="end" className="grafik-yazi">{h}</text>
          </g>
        ))}
        {veri.xEtiket.map(t => (
          <text key={t} x={veri.x(t)} y={Y - 6} textAnchor="middle" className="grafik-yazi">{saat.format(t)}</text>
        ))}
        <path d={veri.yol} className="grafik-hiz" vectorEffect="non-scaling-stroke" />
        {vn && (
          <line x1={veri.x(new Date(vn.zaman).getTime())} x2={veri.x(new Date(vn.zaman).getTime())} y1={ust} y2={Y - alt} className="grafik-imlec" vectorEffect="non-scaling-stroke" />
        )}
      </svg>
      <div className="grafik-bilgi">
        {vn ? <><strong>{vn.hiz} km/s</strong> · {tam.format(new Date(vn.zaman))} · {vn.durum}</> : 'Grafiğin üzerine gelin; tıklayınca o an haritada gösterilir.'}
      </div>
    </div>
  )
}
