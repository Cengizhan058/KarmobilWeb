import { useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { MapContainer, TileLayer, ZoomControl, useMap } from 'react-leaflet'
import { PanelLeftOpen } from 'lucide-react'
import type { LatLngBoundsExpression } from 'leaflet'
import { api, type AracDurumu, type AracKonum, type Grup } from '../api/client.ts'
import { useKullanici } from '../components/Duzen.tsx'
import AracKatmani from '../components/AracKatmani.tsx'
import AracListesi from '../components/AracListesi.tsx'
import AracBilgiPaneli from '../components/AracBilgiPaneli.tsx'

// Eski sitedeki Google katmanlarıyla aynı (index.asp tMapInit).
const KATMANLAR = {
  sokak: { ad: 'Sokak', url: 'https://{s}.google.com/vt/hl=tr&x={x}&y={y}&z={z}', sub: ['mts0', 'mts1', 'mts2', 'mts3'] },
  uydu: { ad: 'Uydu', url: 'https://{s}.google.com/vt/lyrs=y&x={x}&y={y}&z={z}', sub: ['mt0', 'mt1', 'mt2', 'mt3'] },
} as const

type KatmanId = keyof typeof KATMANLAR

// Türkiye geneli; kullanıcının users.MAPX/MAPY sınırı varsa o kullanılır.
const VARSAYILAN_SINIR: LatLngBoundsExpression = [[35.8, 25.6], [42.2, 44.9]]
const YENILEME_MS = 10_000
// Eski sitedeki 'arızalı' sınırı: son sinyal 72 saatten eskiyse (main.js applyFilters gsmfark>=72).
const ARIZALI_SAAT = 72

export type TurFiltre = 'hepsi' | 'arac' | 'kisi'

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

// Araç seçilince ya da Yakınlaştır'a basılınca (istek sayacı artar) araca uçar;
// takip açıksa her konum güncellemesinde haritayı araca kaydırır.
function SeciliyeGit({ arac, istek, takip }: { arac: AracKonum | null; istek: number; takip: boolean }) {
  const map = useMap()
  const son = useRef<string>('')
  useEffect(() => {
    if (!arac || !arac.lat || !arac.lon) return
    const anahtar = `${arac.id}-${istek}`
    if (son.current !== anahtar) {
      son.current = anahtar
      map.flyTo([arac.lat, arac.lon], Math.max(map.getZoom(), 16), { duration: .6 })
    } else if (takip) {
      map.panTo([arac.lat, arac.lon], { animate: true })
    }
  }, [arac, istek, takip, map])
  return null
}

// Liste açılıp kapanınca harita kabının genişliği değişir; Leaflet'e haber verilmezse karolar eksik çizilir.
function BoyutYenile({ anahtar }: { anahtar: unknown }) {
  const map = useMap()
  useEffect(() => {
    const t = setTimeout(() => map.invalidateSize({ animate: false }), 220)
    return () => clearTimeout(t)
  }, [anahtar, map])
  return null
}

function listeTercihi(): boolean {
  try { return localStorage.getItem('km-liste') !== 'kapali' } catch { return true }
}

export default function Harita() {
  const k = useKullanici()
  const [katman, setKatman] = useState<KatmanId>('sokak')
  const [araclar, setAraclar] = useState<AracKonum[]>([])
  const [gruplar, setGruplar] = useState<Grup[]>([])
  const [grupId, setGrupId] = useState<number | null>(null)
  // /?arac=123 ile belirli bir araç seçili açılabilir.
  const [params] = useSearchParams()
  const [seciliId, setSeciliId] = useState<number | null>(() => Number(params.get('arac')) || null)
  const [durumFiltre, setDurumFiltre] = useState<Set<AracDurumu>>(new Set())
  const [hata, setHata] = useState<string | null>(null)
  const [takip, setTakip] = useState(false)
  const [listeAcik, setListeAcik] = useState(listeTercihi)
  const [yaklasIstegi, setYaklasIstegi] = useState(0)
  const [turFiltre, setTurFiltre] = useState<TurFiltre>('hepsi')

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

  // Liste ve harita aynı kapsamı gösterir: görünüm ayarı (arızalılar) + araç/kişi seçimi.
  const kapsam = useMemo(() => araclar.filter(a =>
    !(k.arizalilariGizle && (a.sinyalYasiSaat ?? 0) >= ARIZALI_SAAT) &&
    (turFiltre === 'hepsi' || (turFiltre === 'kisi') === (a.cihazTuru === 1))),
  [araclar, k.arizalilariGizle, turFiltre])
  const kisiVar = araclar.some(a => a.cihazTuru === 1)

  const gorunen = useMemo(
    () => kapsam.filter(a => a.lat && a.lon && (durumFiltre.size === 0 || durumFiltre.has(a.durum))),
    [kapsam, durumFiltre])

  const secili = araclar.find(a => a.id === seciliId) ?? null

  const sec = (a: AracKonum) => {
    if (a.id !== seciliId) setTakip(false)
    setSeciliId(a.id)
    setYaklasIstegi(n => n + 1)
  }

  const listeyiAyarla = (acik: boolean) => {
    setListeAcik(acik)
    try { localStorage.setItem('km-liste', acik ? 'acik' : 'kapali') } catch { /* yok say */ }
  }

  const sinir: LatLngBoundsExpression = k.haritaSiniri
    ? [[k.haritaSiniri.y1, k.haritaSiniri.x1], [k.haritaSiniri.y2, k.haritaSiniri.x2]]
    : VARSAYILAN_SINIR

  const t = KATMANLAR[katman]

  return (
    <div className={'harita-sayfa' + (listeAcik ? '' : ' liste-kapali')}>
      {listeAcik && <AracListesi
        araclar={kapsam}
        kisiVar={kisiVar}
        turFiltre={turFiltre}
        onTur={setTurFiltre}
        gruplar={gruplar}
        grupId={grupId}
        onGrup={setGrupId}
        seciliId={seciliId}
        onSec={sec}
        durumFiltre={durumFiltre}
        onDurumFiltre={setDurumFiltre}
        onKapat={() => listeyiAyarla(false)}
      />}

      <div className="harita-alan">
        <MapContainer bounds={sinir} className="harita" zoomControl={false}>
          <ZoomControl position="bottomright" />
          <BoyutYenile anahtar={listeAcik} />
          <TileLayer key={katman} url={t.url} subdomains={[...t.sub]} maxZoom={20} attribution="Google" />
          <SinirAyarla araclar={araclar} />
          <SeciliyeGit arac={secili} istek={yaklasIstegi} takip={takip} />
          <AracKatmani araclar={gorunen} grupla={k.araclariGrupla} etiket={k.etiket} seciliId={seciliId} onSec={sec} />
        </MapContainer>

        {!listeAcik && (
          <button className="liste-ac" onClick={() => listeyiAyarla(true)} title="Araç listesini göster">
            <PanelLeftOpen size={18} />
            Araçlar <span className="sayi">{kapsam.length}</span>
          </button>
        )}

        <div className="katman-secici">
          {(Object.keys(KATMANLAR) as KatmanId[]).map(id => (
            <button key={id} className={id === katman ? 'secili' : ''} onClick={() => setKatman(id)}>
              {KATMANLAR[id].ad}
            </button>
          ))}
        </div>

        {hata && <div className="harita-uyari">{hata}</div>}
      </div>

      {secili && (
        <AracBilgiPaneli
          arac={secili}
          takipte={takip}
          onTakip={() => setTakip(t => !t)}
          onYaklas={() => setYaklasIstegi(n => n + 1)}
          onKapat={() => { setSeciliId(null); setTakip(false) }}
        />
      )}
    </div>
  )
}

