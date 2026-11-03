import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  X, Crosshair, Navigation, Radio, History, Video, Clock, User, MapPin, Hash, Phone,
  CarFront, Building2, FileText, Lock, LockOpen, Battery, KeyRound, ParkingSquare, TriangleAlert, Route, ExternalLink,
} from 'lucide-react'
import { api, type AracDetay, type AracKonum } from '../api/client.ts'
import { DURUMLAR, sure, tamZaman, temizAdres } from '../lib/arac.ts'
import HizGostergesi from './HizGostergesi.tsx'

type Sekme = 'genel' | 'detay' | 'sokak'

type Props = {
  arac: AracKonum
  takipte: boolean
  onTakip: () => void
  onYaklas: () => void
  onKapat: () => void
}

export default function AracBilgiPaneli({ arac, takipte, onTakip, onYaklas, onKapat }: Props) {
  const navigate = useNavigate()
  const [sekme, setSekme] = useState<Sekme>('genel')
  const [detay, setDetay] = useState<AracDetay | null>(null)
  const [hata, setHata] = useState<string | null>(null)
  const [bildirim, setBildirim] = useState<string | null>(null)
  // Onay bekleyen cihaz komutu: anlık konum ya da motor kilitle / aç.
  const [onay, setOnay] = useState<'konum' | 'kilitle' | 'ac' | null>(null)
  const [gonderiyor, setGonderiyor] = useState(false)

  // Detay (sahip, ruhsat, son kontak/alarm) araç değişince ve son sinyal değişince yeniden alınır.
  useEffect(() => {
    let iptal = false
    setHata(null)
    api.aracDetay(arac.id)
      .then(d => { if (!iptal) setDetay(d) })
      .catch(e => { if (!iptal) setHata(e instanceof Error ? e.message : 'Detay alınamadı.') })
    return () => { iptal = true }
  }, [arac.id, arac.sinyalTarihi])

  useEffect(() => { setSekme('genel'); setOnay(null); setBildirim(null) }, [arac.id])

  useEffect(() => {
    if (!bildirim) return
    const t = setTimeout(() => setBildirim(null), 4000)
    return () => clearTimeout(t)
  }, [bildirim])

  const komutGonder = async () => {
    if (!onay) return
    setGonderiyor(true)
    try {
      const r = onay === 'konum' ? await api.anlikKonumIste(arac.id) : await api.motorBlokaj(arac.id, onay === 'kilitle')
      setBildirim(r.mesaj)
    } catch (e) {
      setBildirim(e instanceof Error ? e.message : 'İstek gönderilemedi.')
    } finally {
      setGonderiyor(false)
      setOnay(null)
    }
  }

  const d = DURUMLAR[arac.durum]
  const adres = temizAdres(arac.adres)

  return (
    <aside className="bilgi-paneli">
      <header className="bilgi-ust" style={{ ['--durum' as string]: d.renk }}>
        <div>
          <div className="bilgi-plaka">{arac.plaka}</div>
          <div className="bilgi-alt">
            <span className="durum-rozet" style={{ background: d.renk }}>{d.ad}</span>
            <span>{arac.kontak ? 'Kontak açık' : 'Kontak kapalı'}</span>
          </div>
        </div>
        <button className="ikon-dugme" title="Kapat" onClick={onKapat}><X size={18} /></button>
      </header>

      <div className="bilgi-gostergeler">
        <HizGostergesi hiz={arac.hiz} />
        <div className="gosterge-kutular">
          <div><small>Bugün</small><strong>{detay?.bugunKm != null ? `${detay.bugunKm.toLocaleString('tr-TR')} km` : arac.gunlukKm != null ? `${Math.round(arac.gunlukKm)} km` : '-'}</strong></div>
          <div><small>Kilometre</small><strong>{arac.kadranKm != null ? Math.round(arac.kadranKm).toLocaleString('tr-TR') : '-'}</strong></div>
          {detay?.batarya != null && (
            <div><small>Batarya</small><strong className="batarya"><Battery size={15} />%{Math.round(detay.batarya)}</strong></div>
          )}
          {detay?.motorBlokaj && (
            <div title={detay.motorBlokaj === 'aktif' ? 'Motor kilitli' : 'Motor açık'}>
              <small>Motor blokaj</small>
              <strong className={detay.motorBlokaj === 'aktif' ? 'kirmizi' : ''}>
                {detay.motorBlokaj === 'aktif' ? <><Lock size={15} />Aktif</> : <><LockOpen size={15} />Pasif</>}
              </strong>
            </div>
          )}
        </div>
      </div>

      <div className="bilgi-eylemler">
        <button onClick={onYaklas}><Crosshair size={16} />Yakınlaştır</button>
        <button className={takipte ? 'etkin' : ''} onClick={onTakip}><Navigation size={16} />{takipte ? 'Takibi bırak' : 'Takip et'}</button>
        <button onClick={() => setOnay('konum')} disabled={gonderiyor}><Radio size={16} />Anlık konum</button>
        {detay?.motorBlokaj && (
          <button onClick={() => setOnay(detay.motorBlokaj === 'aktif' ? 'ac' : 'kilitle')} disabled={gonderiyor}>
            {detay.motorBlokaj === 'aktif' ? <><LockOpen size={16} />Motoru aç</> : <><Lock size={16} />Motoru kilitle</>}
          </button>
        )}
        <button onClick={() => navigate(`/gecmis?arac=${arac.id}`)}><History size={16} />Geçmiş</button>
        {detay?.dvrAdresi && (
          <a className="dugme" href={`http://${detay.dvrAdresi}`} target="_blank" rel="noreferrer"><Video size={16} />Kamera</a>
        )}
      </div>

      {onay && (
        <div className="onay-kutu">
          <p>
            {onay === 'konum' && 'Araca konum gönder komutu iletilecek. Devam edilsin mi?'}
            {onay === 'ac' && 'Araca motoru açma (blokajı kaldırma) komutu iletilecek. Devam edilsin mi?'}
            {onay === 'kilitle' && <>Araca motor kilitleme komutu iletilecek. {arac.hiz > 0
              ? <strong className="kirmizi">Araç şu an {Math.round(arac.hiz)} km/s ile hareket ediyor; seyir halinde motorun durması tehlikeli olabilir.</strong>
              : 'Devam edilsin mi?'}</>}
          </p>
          <div>
            <button className="ana-dugme kucuk" onClick={komutGonder} disabled={gonderiyor}>{gonderiyor ? 'Gönderiliyor…' : 'Gönder'}</button>
            <button className="ikincil-dugme" onClick={() => setOnay(null)}>Vazgeç</button>
          </div>
        </div>
      )}
      {bildirim && <div className="bilgi-bildirim">{bildirim}</div>}

      <nav className="bilgi-sekmeler">
        <button className={sekme === 'genel' ? 'secili' : ''} onClick={() => setSekme('genel')}>Genel</button>
        <button className={sekme === 'detay' ? 'secili' : ''} onClick={() => setSekme('detay')}>Kontak ve alarm</button>
        <button className={sekme === 'sokak' ? 'secili' : ''} onClick={() => setSekme('sokak')}>Sokak görünümü</button>
      </nav>

      <div className="bilgi-icerik">
        {hata && <div className="uyari-kutu">{hata}</div>}

        {sekme === 'genel' && (
          <dl className="bilgi-liste">
            <Satir ikon={Clock} ad="Konum zamanı" deger={tamZaman(arac.gpsTarihi)} />
            <Satir ikon={Clock} ad="Son sinyal" deger={tamZaman(arac.sinyalTarihi)} />
            <Satir ikon={MapPin} ad="Adres" deger={adres} />
            <Satir ikon={Navigation} ad="Koordinat" deger={arac.lat != null ? `${arac.lat.toFixed(6)}, ${arac.lon?.toFixed(6)}` : null} />
            <Satir ikon={User} ad="Sürücü" deger={arac.surucu} />
            <Satir ikon={Building2} ad="Araç sahibi" deger={detay?.aracSahibi} />
            <Satir ikon={CarFront} ad="Araç" deger={[detay?.aracMarkasi, detay?.aracCinsi, detay?.aracTipi].filter(Boolean).join(' · ') || null} />
            <Satir ikon={FileText} ad="Açıklama" deger={detay?.aciklama} />
            <Satir ikon={Hash} ad="Cihaz" deger={detay?.cihazTanimi ?? arac.cihazId} />
            <Satir ikon={Phone} ad="Hat" deger={detay?.gsm} />
          </dl>
        )}

        {sekme === 'detay' && (
          <dl className="bilgi-liste">
            <Satir ikon={Route} ad="Bugünkü mesafe" deger={detay?.bugunKm != null ? `${detay.bugunKm.toLocaleString('tr-TR')} km` : null} />
            {!detay?.kontakAcik && <>
              <Satir ikon={ParkingSquare} ad="Son kontak kapama" deger={tamZaman(detay?.sonKontakKapama ?? null)} />
              <Satir ikon={MapPin} ad="Kapama adresi" deger={detay?.sonKontakKapamaAdresi} />
              <Satir ikon={Clock} ad="Park süresi" deger={sure(detay?.parkSuresiDakika ?? null)} />
            </>}
            <Satir ikon={KeyRound} ad="Son kontak açma" deger={tamZaman(detay?.sonKontakAcma ?? null)} />
            <Satir ikon={MapPin} ad="Açma adresi" deger={detay?.sonKontakAcmaAdresi} />
            <Satir ikon={Clock} ad="Kontak açık kalma" deger={sure(detay?.kontakAcikSureDakika ?? null)} />
            <Satir ikon={TriangleAlert} ad="Son alarm" deger={detay?.sonAlarm ? `${detay.sonAlarm} · ${tamZaman(detay.sonAlarmTarihi)}` : 'Son 24 saatte alarm yok'} />
            <Satir ikon={MapPin} ad="Alarm adresi" deger={detay?.sonAlarmAdresi} />
          </dl>
        )}

        {sekme === 'sokak' && arac.lat != null && arac.lon != null && (
          <div className="sokak-gorunumu">
            <iframe
              title="Sokak görünümü"
              src={`https://maps.google.com/maps?layer=c&cbll=${arac.lat},${arac.lon}&cbp=11,${Math.round(arac.yon)},0,0,0&output=svembed`}
              loading="lazy"
              referrerPolicy="no-referrer-when-downgrade"
            />
            <a href={`https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=${arac.lat},${arac.lon}&heading=${Math.round(arac.yon)}`} target="_blank" rel="noreferrer">
              <ExternalLink size={14} />Google Haritalar'da aç
            </a>
          </div>
        )}
      </div>
    </aside>
  )
}

function Satir({ ikon: Ikon, ad, deger }: { ikon: typeof Clock; ad: string; deger: string | null | undefined }) {
  return (
    <div className="bilgi-satir">
      <dt><Ikon size={14} />{ad}</dt>
      <dd>{deger && deger !== '-' ? deger : <span className="soluk">-</span>}</dd>
    </div>
  )
}
