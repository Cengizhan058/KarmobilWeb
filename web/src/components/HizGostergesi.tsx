// Yarım daire hız göstergesi (eski bilgi ekranındaki JustGage yerine, 0-180 km/s).
export default function HizGostergesi({ hiz, azami = 180 }: { hiz: number; azami?: number }) {
  const oran = Math.max(0, Math.min(1, hiz / azami))
  const r = 52
  const cevre = Math.PI * r
  const renk = hiz >= 120 ? '#d9534f' : hiz >= 90 ? '#e0a526' : '#2fa36b'

  return (
    <svg className="hiz-gostergesi" viewBox="0 0 130 78" role="img" aria-label={`Hız ${Math.round(hiz)} km/s`}>
      <path d="M13 70 A52 52 0 0 1 117 70" fill="none" stroke="var(--cizgi)" strokeWidth="11" strokeLinecap="round" />
      <path
        d="M13 70 A52 52 0 0 1 117 70"
        fill="none"
        stroke={renk}
        strokeWidth="11"
        strokeLinecap="round"
        strokeDasharray={`${cevre * oran} ${cevre}`}
      />
      <text x="65" y="60" textAnchor="middle" className="hiz-deger">{Math.round(hiz)}</text>
      <text x="65" y="75" textAnchor="middle" className="hiz-birim">km/s</text>
    </svg>
  )
}
