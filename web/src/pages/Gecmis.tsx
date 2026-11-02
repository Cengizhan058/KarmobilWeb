import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  ChevronLeft, ChevronRight, Search, Play, Pause, SkipBack, FileSpreadsheet, Printer, Route, Gauge, Clock, ParkingSquare, MapPin,
} from 'lucide-react'
import { api, type AracKonum, type Gecmis as GecmisVeri, type GecmisNokta } from '../api/client.ts'
import GecmisHarita from '../components/GecmisHarita.tsx'
import HizGrafigi from '../components/HizGrafigi.tsx'
import { sure } from '../lib/arac.ts'
import { csvIndir, dosyaAdiParcasi } from '../lib/disaAktar.ts'

type Sekme = 'harita' | 'liste' | 'grafik'

const tam = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'medium' })
const iki = (n: number) => String(n).padStart(2, '0')
/** <input type="datetime-local"> biçimi: 2026-11-02T08:30 */
const yerelGirdi = (d: Date) => `${d.getFullYear()}-${iki(d.getMonth() + 1)}-${iki(d.getDate())}T${iki(d.getHours())}:${iki(d.getMinutes())}`
const gunKaydir = (s: string, gun: number) => { const d = new Date(s); d.setDate(d.getDate() + gun); return yerelGirdi(d) }

/** Liste satırı: tek kayıt ya da (park gruplama açıkken) bir duraklama. */
type Satir =
  | { tur: 'nokta'; i: number; n: GecmisNokta }
  | { tur: 'durak'; i: number; n: GecmisNokta; dakika: number }

export default function Gecmis() {
  const [params] = useSearchParams()
  const [araclar, setAraclar] = useState<AracKonum[]>([])
  const [aracId, setAracId] = useState<number | null>(() => Number(params.get('arac')) || null)
  const [baslangic, setBaslangic] = useState(() => { const d = new Date(); d.setHours(0, 0, 0, 0); return yerelGirdi(d) })
  const [bitis, setBitis] = useState(() => yerelGirdi(new Date()))
  const [poi, setPoi] = useState(false)
  const [parkGrupla, setParkGrupla] = useState(true)
  const [durakEsik, setDurakEsik] = useState(5)

  const [veri, setVeri] = useState<GecmisVeri | null>(null)
  const [yukleniyor, setYukleniyor] = useState(false)
  const [hata, setHata] = useState<string | null>(null)
  const [sekme, setSekme] = useState<Sekme>('harita')
  const [secili, setSecili] = useState<number | null>(null)
  const [kaydir, setKaydir] = useState(0)
  const [oynuyor, setOynuyor] = useState(false)
  const [carpan, setCarpan] = useState(10)

  useEffect(() => { api.araclar().then(setAraclar).catch(() => { /* liste boş kalır */ }) }, [])

  const listele = async (b = baslangic, s = bitis) => {
    if (!aracId) { setHata('Araç seçiniz.'); return }
    setHata(null); setYukleniyor(true); setOynuyor(false)
    try {
      const v = await api.gecmis(aracId, `${b}:00`, `${s}:00`, poi)
      setVeri(v)
      setSecili(v.noktalar.length ? 0 : null)
    } catch (e) {
      setHata(e instanceof Error ? e.message : 'Kayıtlar alınamadı.')
    } finally {
      setYukleniyor(false)
    }
  }

  // Panelden "Geçmiş" ile gelindiyse bugünü hemen getir.
  useEffect(() => { if (aracId && params.get('arac')) listele() }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const gunDegistir = (gun: number) => {
    const b = gunKaydir(baslangic, gun), s = gunKaydir(bitis, gun)
    setBaslangic(b); setBitis(s)
    listele(b, s)
  }

  // Oynatma: her adımda bir sonraki kayda geçer; hız çarpanı adım aralığını kısaltır.
  useEffect(() => {
    if (!oynuyor || !veri) return
    const t = setInterval(() => {
      setSecili(i => {
        const sonraki = (i ?? 0) + 1
        if (sonraki >= veri.noktalar.length) { setOynuyor(false); return i }
        return sonraki
      })
      setKaydir(n => n + 1)
    }, Math.max(30, 1000 / carpan))
    return () => clearInterval(t)
  }, [oynuyor, carpan, veri])

  const satirlar = useMemo<Satir[]>(() => {
    if (!veri) return []
    const n = veri.noktalar
    if (!parkGrupla) return n.map((x, i) => ({ tur: 'nokta', i, n: x }))
    const sonuc: Satir[] = []
    for (let i = 0; i < n.length; i++) {
      // Kontak/alarm gibi olay kayıtları park sırasında da görünsün; düz "Park halinde" kayıtları birleşir.
      if (n[i].hiz === 0 && n[i].durum === 'Park halinde') {
        let j = i
        while (j + 1 < n.length && n[j + 1].hiz === 0 && n[j + 1].durum === 'Park halinde') j++
        const bitisZamani = j + 1 < n.length ? n[j + 1].zaman : n[j].zaman
        const dakika = Math.max(1, Math.round((new Date(bitisZamani).getTime() - new Date(n[i].zaman).getTime()) / 60000))
        sonuc.push({ tur: 'durak', i, n: n[i], dakika })
        i = j
      } else sonuc.push({ tur: 'nokta', i, n: n[i] })
    }
    return sonuc
  }, [veri, parkGrupla])

  const ozet = useMemo(() => {
    if (!veri || veri.noktalar.length === 0) return null
    const parkDk = veri.duraklar.reduce((t, d) => t + d.dakika, 0)
    const ilk = new Date(veri.noktalar[0].zaman).getTime()
    const son = new Date(veri.noktalar[veri.noktalar.length - 1].zaman).getTime()
    return { parkDk, hareketDk: Math.max(0, Math.round((son - ilk) / 60000) - parkDk), uzunDurak: veri.duraklar.filter(d => d.dakika >= durakEsik).length }
  }, [veri, durakEsik])

  const sec = (i: number, haritaya = false) => {
    setSecili(i); setKaydir(n => n + 1); setOynuyor(false)
    if (haritaya) setSekme('harita')
  }

  const excel = () => {
    if (!veri) return
    csvIndir(
      `gecmis-${dosyaAdiParcasi(veri.plaka)}-${baslangic.slice(0, 10)}.csv`,
      ['Tarih', 'Plaka', 'Sürücü', 'Konum', 'Kontak', 'Hız (km/s)', 'Durum', 'Rapor kodu', 'Koordinat', 'Kadran km'],
      satirlar.map(s => [
        tam.format(new Date(s.n.zaman)), veri.plaka, s.n.surucu ?? '', s.n.adres ?? '', s.n.kontak ? 'AÇIK' : 'KAPALI',
        s.tur === 'durak' ? 0 : s.n.hiz, s.tur === 'durak' ? `${s.dakika} dakika durdu` : s.n.durum, s.n.raporKodu,
        `${s.n.lat.toFixed(6)},${s.n.lon.toFixed(6)}`, s.n.kadranKm ?? '',
      ]))
  }

  const yazdir = () => { setSekme('liste'); setTimeout(() => window.print(), 100) }

  const gonder = (e: FormEvent) => { e.preventDefault(); listele() }
  const sn = secili != null && veri ? veri.noktalar[secili] : null

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form">
        <form onSubmit={gonder}>
          <h3>Geçmiş izleme</h3>
          <label className="alan">
            <span>Araç</span>
            <select className="secim" value={aracId ?? ''} onChange={e => setAracId(e.target.value ? Number(e.target.value) : null)}>
              <option value="">Araç seçiniz</option>
              {araclar.map(a => <option key={a.id} value={a.id}>{a.plaka}</option>)}
            </select>
          </label>
          <label className="alan">
            <span>Başlangıç</span>
            <input className="secim" type="datetime-local" value={baslangic} onChange={e => setBaslangic(e.target.value)} required />
          </label>
          <label className="alan">
            <span>Bitiş</span>
            <input className="secim" type="datetime-local" value={bitis} onChange={e => setBitis(e.target.value)} required />
          </label>
          <div className="gun-dugmeleri">
            <button type="button" className="ikincil-dugme" onClick={() => gunDegistir(-1)}><ChevronLeft size={16} />Önceki gün</button>
            <button type="button" className="ikincil-dugme" onClick={() => gunDegistir(1)}>Sonraki gün<ChevronRight size={16} /></button>
          </div>
          <label className="onay-satir"><input type="checkbox" checked={parkGrupla} onChange={e => setParkGrupla(e.target.checked)} />Park halindeki konumları grupla</label>
          <label className="onay-satir"><input type="checkbox" checked={poi} onChange={e => setPoi(e.target.checked)} />Noktalara (POI) göre konum yaz</label>
          <button className="ana-dugme" type="submit" disabled={yukleniyor}>
            {yukleniyor ? <span className="donen" /> : <><Search size={16} />Listele</>}
          </button>
          {hata && <div className="uyari-kutu">{hata}</div>}
        </form>

        {veri && ozet && (
          <div className="gecmis-ozet">
            <div><Route size={16} /><small>Toplam mesafe</small><strong>{veri.toplamKm.toLocaleString('tr-TR')} km</strong></div>
            <div><Gauge size={16} /><small>En yüksek hız</small><strong>{veri.azamiHiz} km/s</strong></div>
            <div><Clock size={16} /><small>Hareket süresi</small><strong>{sure(ozet.hareketDk)}</strong></div>
            <div><ParkingSquare size={16} /><small>Duraklama</small><strong>{sure(ozet.parkDk)}</strong></div>
          </div>
        )}
      </aside>

      <section className="gecmis-icerik">
        <div className="gecmis-ust">
          <nav className="bilgi-sekmeler">
            <button className={sekme === 'harita' ? 'secili' : ''} onClick={() => setSekme('harita')}>Harita</button>
            <button className={sekme === 'liste' ? 'secili' : ''} onClick={() => setSekme('liste')}>Liste{veri ? ` (${satirlar.length})` : ''}</button>
            <button className={sekme === 'grafik' ? 'secili' : ''} onClick={() => setSekme('grafik')}>Hız grafiği</button>
          </nav>
          {veri && (
            <div className="gecmis-araclar">
              <button className="ikincil-dugme" onClick={excel}><FileSpreadsheet size={16} />Excel</button>
              <button className="ikincil-dugme" onClick={yazdir}><Printer size={16} />Yazdır / PDF</button>
            </div>
          )}
        </div>

        {!veri && <div className="yapim"><MapPin size={36} /><p>Araç ve tarih aralığı seçip Listele'ye basın.</p></div>}

        {veri && veri.noktalar.length === 0 && <div className="yapim"><p>Bu aralıkta kayıt yok.</p></div>}

        {veri && veri.noktalar.length > 0 && sekme === 'harita' && (
          <div className="gecmis-harita">
            <GecmisHarita noktalar={veri.noktalar} duraklar={veri.duraklar} durakEsikDakika={durakEsik} seciliIndeks={secili} kaydirIstegi={kaydir} />
            <div className="oynatici">
              <button className="ikon-dugme" title="Başa dön" onClick={() => sec(0)}><SkipBack size={18} /></button>
              <button className="ikon-dugme oynat" title={oynuyor ? 'Durdur' : 'Oynat'} onClick={() => setOynuyor(o => !o)}>{oynuyor ? <Pause size={18} /> : <Play size={18} />}</button>
              <input type="range" min={0} max={veri.noktalar.length - 1} value={secili ?? 0} onChange={e => sec(Number(e.target.value))} />
              <select value={carpan} onChange={e => setCarpan(Number(e.target.value))} title="Oynatma hızı">
                {[1, 5, 10, 25, 50].map(c => <option key={c} value={c}>{c}×</option>)}
              </select>
              <select value={durakEsik} onChange={e => setDurakEsik(Number(e.target.value))} title="Haritada gösterilecek duraklamalar">
                {[1, 5, 15, 30, 60].map(d => <option key={d} value={d}>{d} dk+ duraklar</option>)}
              </select>
              {sn && <span className="oynatici-bilgi"><strong>{sn.hiz} km/s</strong> · {tam.format(new Date(sn.zaman))} · {sn.durum}{sn.adres ? ` · ${sn.adres}` : ''}</span>}
            </div>
            <div className="hiz-lejant"><span style={{ background: '#2fa36b' }} />0–49 <span style={{ background: '#e0a526' }} />50–89 <span style={{ background: '#d9534f' }} />90+ km/s · {ozet?.uzunDurak ?? 0} duraklama</div>
          </div>
        )}

        {veri && veri.noktalar.length > 0 && sekme === 'liste' && (
          <div className="tablo-kap yazdirilacak">
            <div className="yazdir-baslik">{veri.plaka} · {tam.format(new Date(veri.baslangic))} – {tam.format(new Date(veri.bitis))} · {veri.toplamKm.toLocaleString('tr-TR')} km</div>
            <table className="tablo">
              <thead>
                <tr><th>Tarih</th><th>Hız</th><th>Durum</th><th>Kontak</th><th>Konum</th><th>Sürücü</th><th>Kadran km</th></tr>
              </thead>
              <tbody>
                {satirlar.map(s => (
                  <tr key={`${s.tur}-${s.n.id}`} className={(s.i === secili ? 'secili ' : '') + (s.tur === 'durak' ? 'durak' : '')} onDoubleClick={() => sec(s.i, true)} onClick={() => sec(s.i)}>
                    <td className="sayi-hucre">{tam.format(new Date(s.n.zaman))}</td>
                    <td className="sayi-hucre">{s.tur === 'durak' ? 0 : s.n.hiz}</td>
                    <td>{s.tur === 'durak' ? `${sure(s.dakika)} durdu` : s.n.durum}</td>
                    <td>{s.n.kontak ? 'Açık' : 'Kapalı'}</td>
                    <td>{s.n.adres ?? '-'}</td>
                    <td>{s.n.surucu ?? ''}</td>
                    <td className="sayi-hucre">{s.n.kadranKm != null ? Math.round(s.n.kadranKm).toLocaleString('tr-TR') : ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <p className="tablo-not">Satıra çift tıklayınca o an haritada gösterilir.</p>
          </div>
        )}

        {veri && veri.noktalar.length > 0 && sekme === 'grafik' && (
          <HizGrafigi noktalar={veri.noktalar} seciliIndeks={secili} onSec={i => sec(i, true)} />
        )}
      </section>
    </div>
  )
}
