import { useEffect, useMemo, useRef, useState } from 'react'
import { MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet'
import L, { type LatLngBoundsExpression } from 'leaflet'
import { Gauge, Clock, User, MapPin, Route, Navigation, Lock } from 'lucide-react'
import { api, type AracDurumu, type AracKonum, type Grup } from '../api/client.ts'
import { useKullanici } from '../components/Duzen.tsx'
import AracListesi from '../components/AracListesi.tsx'
import { DURUMLAR, tamZaman, temizAdres } from '../lib/arac.ts'

// Eski sitedeki Google katmanlarıyla aynı (index.asp tMapInit).
const KATMANLAR = {
  sokak: { ad: 'Sokak', url: 'https://{s}.google.com/vt/hl=tr&x={x}&y={y}&z={z}', sub: ['mts0', 'mts1', 'mts2', 'mts3'] },
  uydu: { ad: 'Uydu', url: 'https://{s}.google.com/vt/lyrs=y&x={x}&y={y}&z={z}', sub: ['mt0', 'mt1', 'mt2', 'mt3'] },
} as const

type KatmanId = keyof typeof KATMANLAR

// Türkiye geneli; kullanıcının users.MAPX/MAPY sınırı varsa o kullanılır.
const VARSAYILAN_SINIR: LatLngBoundsExpression = [[35.8, 25.6], [42.2, 44.9]]
const YENILEME_MS = 10_000

function aracIkonu(a: AracKonum, secili: boolean) {
  const renk = DURUMLAR[a.durum].renk
  // Hareketli araçta yön oku, diğerlerinde nokta.
  const isaret = a.durum === 'Hareketli'
    ? `<svg viewBox="0 0 24 24" width="16" height="16" style="transform:rotate(${a.yon}deg)"><path d="M12 2 L20 21 L12 16 L4 21 Z" fill="#fff"/></svg>`
    : '<span class="ic-nokta"></span>'
  return L.divIcon({
    className: 'arac-isaret' + (secili ? ' secili' : ''),
    html: `<div class="isaret-daire" style="background:${renk}">${isaret}</div><div class="isaret-plaka">${a.plaka}</div>`,
    iconSize: [0, 0],
    iconAnchor: [0, 0],
    popupAnchor: [0, -16],
  })
}

function SinirAyarla({ araclar }: { araclar: AracKonum[] }) {
  const map = useMap()
  const yapildi = useRef(false)
  useEffect(() => {
    if (yapildi.current) return
    const noktalar = araclar.filter(a => a.lat && a.lon).map(a => [a.lat!, a.lon!] as [number, number])
    if (noktalar.length === 0) return
    yapildi.current = true
    if (noktalar.length === 1) map.setView(noktalar[0], 15)
    else map.fitBounds(noktalar, { padding: [60, 60], maxZoom: 15 })
  }, [araclar, map])
  return null
}

function SeciliyeGit({ arac }: { arac: AracKonum | null }) {
  const map = useMap()
  const sonId = useRef<number | null>(null)
  useEffect(() => {
    if (!arac || !arac.lat || !arac.lon || sonId.current === arac.id) return
    sonId.current = arac.id
    map.flyTo([arac.lat, arac.lon], Math.max(map.getZoom(), 16), { duration: .6 })
  }, [arac, map])
  return null
}

export default function Harita() {
  const k = useKullanici()
  const [katman, setKatman] = useState<KatmanId>('sokak')
  const [araclar, setAraclar] = useState<AracKonum[]>([])
  const [gruplar, setGruplar] = useState<Grup[]>([])
  const [grupId, setGrupId] = useState<number | null>(null)
  const [seciliId, setSeciliId] = useState<number | null>(null)
  const [durumFiltre, setDurumFiltre] = useState<Set<AracDurumu>>(new Set())
  const [hata, setHata] = useState<string | null>(null)
  const isaretler = useRef<Record<number, L.Marker | null>>({})

  useEffect(() => { api.gruplar().then(setGruplar).catch(() => { /* grup yoksa seçim gizlenir */ }) }, [])

  useEffect(() => {
    let iptal = false
    let zamanlayici: ReturnType<typeof setTimeout>
    const yukle = async () => {
      try {
        const veri = await api.araclar(grupId)
        if (!iptal) { setAraclar(veri); setHata(null) }
      } catch (e) {
        if (!iptal) setHata(e instanceof Error ? e.message : 'Araçlar alınamadı.')
      } finally {
        if (!iptal) zamanlayici = setTimeout(yukle, YENILEME_MS)
      }
    }
    yukle()
    return () => { iptal = true; clearTimeout(zamanlayici) }
  }, [grupId])

  const gorunen = useMemo(
    () => araclar.filter(a => a.lat && a.lon && (durumFiltre.size === 0 || durumFiltre.has(a.durum))),
    [araclar, durumFiltre])

  const secili = araclar.find(a => a.id === seciliId) ?? null

  const sec = (a: AracKonum) => {
    setSeciliId(a.id)
    // Popup, işaret haritaya gelince açılsın.
    setTimeout(() => isaretler.current[a.id]?.openPopup(), 650)
  }

  const sinir: LatLngBoundsExpression = k.haritaSiniri
    ? [[k.haritaSiniri.y1, k.haritaSiniri.x1], [k.haritaSiniri.y2, k.haritaSiniri.x2]]
    : VARSAYILAN_SINIR

  const t = KATMANLAR[katman]

  return (
    <div className="harita-sayfa">
      <AracListesi
        araclar={araclar}
        gruplar={gruplar}
        grupId={grupId}
        onGrup={setGrupId}
        seciliId={seciliId}
        onSec={sec}
        durumFiltre={durumFiltre}
        onDurumFiltre={setDurumFiltre}
      />

      <div className="harita-alan">
        <MapContainer bounds={sinir} className="harita" zoomControl={true}>
          <TileLayer key={katman} url={t.url} subdomains={[...t.sub]} maxZoom={20} attribution="Google" />
          <SinirAyarla araclar={araclar} />
          <SeciliyeGit arac={secili} />
          {gorunen.map(a => (
            <Marker
              key={a.id}
              position={[a.lat!, a.lon!]}
              icon={aracIkonu(a, a.id === seciliId)}
              zIndexOffset={a.id === seciliId ? 1000 : 0}
              ref={m => { isaretler.current[a.id] = m }}
              eventHandlers={{ click: () => setSeciliId(a.id) }}
            >
              <Popup className="arac-popup" maxWidth={320}>
                <AracKarti a={a} />
              </Popup>
            </Marker>
          ))}
        </MapContainer>

        <div className="katman-secici">
          {(Object.keys(KATMANLAR) as KatmanId[]).map(id => (
            <button key={id} className={id === katman ? 'secili' : ''} onClick={() => setKatman(id)}>
              {KATMANLAR[id].ad}
            </button>
          ))}
        </div>

        {hata && <div className="harita-uyari">{hata}</div>}
      </div>
    </div>
  )
}

function AracKarti({ a }: { a: AracKonum }) {
  const d = DURUMLAR[a.durum]
  return (
    <div className="arac-kart">
      <div className="kart-ust">
        <strong>{a.plaka}</strong>
        <span className="durum-rozet" style={{ background: d.renk }}>{d.ad}</span>
      </div>
      <ul>
        <li><Gauge size={14} /> {Math.round(a.hiz)} km/s {a.kontak ? '· kontak açık' : '· kontak kapalı'}</li>
        <li><Clock size={14} /> {tamZaman(a.sinyalTarihi)}</li>
        {a.surucu && <li><User size={14} /> {a.surucu}</li>}
        <li><MapPin size={14} /> {temizAdres(a.adres) ?? 'Adres yok'}</li>
        {a.gunlukKm != null && <li><Route size={14} /> Bugün {Math.round(a.gunlukKm)} km{a.kadranKm != null ? ` · kadran ${Math.round(a.kadranKm).toLocaleString('tr-TR')} km` : ''}</li>}
        {a.lat != null && <li><Navigation size={14} /> {a.lat.toFixed(5)}, {a.lon?.toFixed(5)}</li>}
        {a.motorBlokeli && <li className="kirmizi"><Lock size={14} /> Motor blokesi aktif</li>}
      </ul>
    </div>
  )
}
