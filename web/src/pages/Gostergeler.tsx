import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Gauge, RefreshCw } from 'lucide-react'
import { api, type GostergeSirasi, type Gostergeler as Veri } from '../api/client.ts'
import { DURUMLAR } from '../lib/arac.ts'

const sayi = (n: number, basamak = 0) => n.toLocaleString('tr-TR', { maximumFractionDigits: basamak })
const yuzde = (n: number, t: number) => (t > 0 ? Math.round((n * 100) / t) : 0)

type Parca = { ad: string; deger: number; renk: string }

/** Parçaların bütüne oranı: tek ince yığılmış çubuk + altında sayılı açıklama (renk tek başına anlam taşımaz). */
function OranCubugu({ parcalar, birim = '' }: { parcalar: Parca[]; birim?: string }) {
  const toplam = parcalar.reduce((s, p) => s + p.deger, 0)
  return (
    <>
      <div className="oran-cubugu" role="img" aria-label={parcalar.map(p => `${p.ad} ${sayi(p.deger, 1)}${birim}`).join(', ')}>
        {toplam > 0
          ? parcalar.filter(p => p.deger > 0).map(p => (
            <span key={p.ad} style={{ flexGrow: p.deger, background: p.renk }} title={`${p.ad}: ${sayi(p.deger, 1)}${birim} (%${yuzde(p.deger, toplam)})`} />
          ))
          : <span className="bos" />}
      </div>
      <ul className="gosterge-liste">
        {parcalar.map(p => (
          <li key={p.ad}>
            <span className="nokta" style={{ background: p.renk }} />
            <span>{p.ad}</span>
            <strong>{sayi(p.deger, 1)}{birim}</strong>
            <span className="soluk">%{yuzde(p.deger, toplam)}</span>
          </li>
        ))}
      </ul>
    </>
  )
}

/** İlk beş araç: tek renkli, sıfırdan başlayan ince çubuklar; değer yazıyla da yazılı. */
function Siralama({ liste, birim }: { liste: GostergeSirasi[]; birim: string }) {
  if (liste.length === 0) return <p className="soluk kucuk-not">Bugün kayıt yok.</p>
  const enBuyuk = Math.max(...liste.map(x => x.deger), 1)
  return (
    <ol className="gosterge-sira">
      {liste.map(x => (
        <li key={x.id} title={`${x.plaka}: ${sayi(x.deger, 1)} ${birim}`}>
          <Link to={`/gecmis?arac=${x.id}`}>{x.plaka}</Link>
          <span className="sira-cubuk"><span style={{ width: `${(x.deger * 100) / enBuyuk}%` }} /></span>
          <strong>{sayi(x.deger, 1)} {birim}</strong>
        </li>
      ))}
    </ol>
  )
}

/** Göstergeler (eski dashboard.asp): bugünkü filo özeti. İki dakikada bir yenilenir. */
export default function Gostergeler() {
  const [veri, setVeri] = useState<Veri | null>(null)
  const [hata, setHata] = useState<string | null>(null)
  const [yukleniyor, setYukleniyor] = useState(false)
  const [zaman, setZaman] = useState<Date | null>(null)

  const yukle = () => {
    setYukleniyor(true)
    api.gostergeler()
      .then(v => { setVeri(v); setHata(null); setZaman(new Date()) })
      .catch(e => setHata(e.message))
      .finally(() => setYukleniyor(false))
  }
  useEffect(() => {
    yukle()
    const t = setInterval(yukle, 120_000)
    return () => clearInterval(t)
  }, [])

  const d = veri?.durum
  return (
    <div className="tek-sayfa">
      <div className="islem-baslik">
        <h2><Gauge size={20} /> Göstergeler</h2>
        <div className="grup-arac-ust">
          {zaman && <span className="soluk kucuk-not">Güncelleme {zaman.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}</span>}
          <button className="ikincil-dugme" onClick={yukle} disabled={yukleniyor}><RefreshCw size={15} className={yukleniyor ? 'donuyor' : ''} />Yenile</button>
        </div>
      </div>
      {hata && <div className="uyari-kutu">{hata}</div>}
      {!veri && !hata && <div className="yapim kucuk"><span className="donen" /></div>}

      {veri && d && (
        <div className="gosterge-izgara">
          <section className="gosterge-kart">
            <h3>Genel durum</h3>
            <div className="buyuk-sayi">{sayi(d.toplam)} <small>araç</small></div>
            <OranCubugu parcalar={[
              { ad: DURUMLAR.Hareketli.ad, deger: d.hareketli, renk: DURUMLAR.Hareketli.renk },
              { ad: DURUMLAR.Rolanti.ad, deger: d.rolanti, renk: DURUMLAR.Rolanti.renk },
              { ad: DURUMLAR.Park.ad, deger: d.park, renk: DURUMLAR.Park.renk },
              { ad: DURUMLAR.VeriYok.ad, deger: d.veriYok, renk: DURUMLAR.VeriYok.renk },
            ]} />
            <p className="soluk kucuk-not">Veri yok: son 25 dakikada sinyal gelmeyen araçlar.</p>
            <Link className="kart-baglanti" to="/">Haritada göster</Link>
          </section>

          <section className="gosterge-kart">
            <h3>Mesai performansı <small className="soluk">bugün</small></h3>
            <div className="buyuk-sayi">{sayi(veri.mesafe.toplamKm, 1)} <small>km</small></div>
            <OranCubugu birim=" km" parcalar={[
              { ad: 'Mesai içi', deger: veri.mesafe.mesaiIciKm, renk: 'var(--vurgu)' },
              { ad: 'Mesai dışı', deger: veri.mesafe.mesaiDisiKm, renk: 'var(--bar-koyu)' },
            ]} />
            <Link className="kart-baglanti" to="/rapor/report_workhours">Toplam mesai km raporu</Link>
          </section>

          <section className="gosterge-kart">
            <h3>Alarm paneli <small className="soluk">bugün</small></h3>
            <ul className="gosterge-liste sade">
              {veri.alarmlar.map(a => (
                <li key={a.kod}><span>{a.ad}</span><strong className={a.adet > 0 ? 'alarm-var' : ''}>{sayi(a.adet)}</strong></li>
              ))}
            </ul>
            <Link className="kart-baglanti" to="/rapor/report_alarms">Alarm / ihlal raporu</Link>
          </section>

          <section className="gosterge-kart">
            <h3>En fazla hız yapanlar <small className="soluk">bugün</small></h3>
            <div className="ikili-sayi">
              <div><small className="soluk">En yüksek</small><strong>{sayi(veri.hiz.maks)} km/s</strong></div>
              <div><small className="soluk">Ortalama</small><strong>{sayi(veri.hiz.ortalama)} km/s</strong></div>
            </div>
            <Siralama liste={veri.hiz.enCok} birim="km/s" />
          </section>

          <section className="gosterge-kart">
            <h3>En fazla mesafe yapanlar <small className="soluk">bugün</small></h3>
            <div className="ikili-sayi">
              <div><small className="soluk">Toplam</small><strong>{sayi(veri.mesafe.toplamKm, 1)} km</strong></div>
              <div><small className="soluk">Araç başı ortalama</small><strong>{sayi(veri.mesafe.ortalamaKm, 1)} km</strong></div>
            </div>
            <Siralama liste={veri.mesafe.enCok} birim="km" />
            <Link className="kart-baglanti" to="/rapor/report_distance_tbl">Aylık km raporu</Link>
          </section>
        </div>
      )}
    </div>
  )
}
