import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Bell, Mail, MessageSquare, Save, Search } from 'lucide-react'
import { api, type AlarmAyari, type AlarmTanimi, type BildirimTanimlari } from '../api/client.ts'

const SAATLER = ['-', ...Array.from({ length: 24 }, (_, i) => String(i))]

const bosAyar = (kod: string): AlarmAyari =>
  ({ kod, durum: 0, sayi1: null, sayi2: null, bas: null, bit: null, rota: null, bolgeler: null, eposta: false, sms: false })

const ayni = (a: AlarmAyari, b: AlarmAyari) => JSON.stringify(a) === JSON.stringify(b)

/**
 * Ayarlar → Bildirim Ayarları (eski alarm_settings). Bir araca tıklayınca tanımları yüklenir; yalnız değiştirilen
 * alarmlar işaretli araçların hepsine kaydedilir (eski formdaki alarm onay kutuları gibi). Üstte e-posta / SMS alıcıları.
 */
export default function BildirimAyarlari() {
  const [tanim, setTanim] = useState<BildirimTanimlari | null>(null)
  const [araclar, setAraclar] = useState<{ id: number; plaka: string }[]>([])
  const [secili, setSecili] = useState<Set<number>>(new Set())
  const [kaynak, setKaynak] = useState<number | null>(null)
  const [ilk, setIlk] = useState<Record<string, AlarmAyari>>({})
  const [ayar, setAyar] = useState<Record<string, AlarmAyari>>({})
  const [ara, setAra] = useState('')
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)
  const [params] = useSearchParams()

  useEffect(() => {
    api.bildirimTanimlari().then(setTanim).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
    api.araclar().then(a => setAraclar([...a].sort((x, y) => x.plaka.localeCompare(y.plaka, 'tr')))).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }, [])

  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q)) : araclar
  }, [araclar, ara])

  const yukle = (id: number) => {
    if (!tanim) return
    setKaynak(id); setMesaj(null)
    setSecili(s => new Set(s).add(id))
    api.bildirimAyarlari(id).then(list => {
      const d = Object.fromEntries(tanim.alarmlar.map(t => [t.kod, list.find(x => x.kod === t.kod) ?? bosAyar(t.kod)]))
      setIlk(d); setAyar(d)
    }).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }

  // ?arac=95 ile açılınca o aracın tanımları yüklenir.
  useEffect(() => {
    const id = Number(params.get('arac'))
    if (tanim && id && !kaynak) yukle(id)
  }, [tanim]) // eslint-disable-line react-hooks/exhaustive-deps

  const degisenler = tanim ? tanim.alarmlar.filter(t => ayar[t.kod] && ilk[t.kod] && !ayni(ayar[t.kod], ilk[t.kod])) : []

  const kaydet = async () => {
    if (!degisenler.length || !secili.size) return
    const adet = secili.size
    if (!confirm(`${degisenler.map(t => t.baslik).join(', ')} ${adet} araca kaydedilecek. Onaylıyor musunuz?`)) return
    setMesgul(true); setMesaj(null)
    try {
      const r = await api.bildirimKaydet([...secili], degisenler.map(t => ayar[t.kod]))
      setMesaj({ tur: 'ok', metin: r.mesaj })
      setIlk(ayar)
    } catch (e) { setMesaj({ tur: 'hata', metin: (e as Error).message }) } finally { setMesgul(false) }
  }

  const guncelle = (kod: string, d: Partial<AlarmAyari>) => setAyar(a => ({ ...a, [kod]: { ...a[kod], ...d } }))

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form yonetim-liste">
        <h3>Araçlar</h3>
        <label className="arama-kutu"><Search size={15} /><input placeholder="Plaka ara" value={ara} onChange={e => setAra(e.target.value)} /></label>
        <div className="grup-arac-ust">
          <button type="button" className="ikincil-dugme" onClick={() => setSecili(new Set([...secili, ...liste.map(a => a.id)]))}>Tümünü seç</button>
          <button type="button" className="ikincil-dugme" onClick={() => setSecili(new Set())}>Temizle</button>
        </div>
        <ul className="islem-liste uzun">
          {liste.map(a => (
            <li key={a.id} className={a.id === kaynak ? 'secili' : ''} onClick={() => yukle(a.id)}>
              <input type="checkbox" checked={secili.has(a.id)} onClick={e => e.stopPropagation()}
                onChange={e => setSecili(s => { const y = new Set(s); if (e.target.checked) y.add(a.id); else y.delete(a.id); return y })} />
              <span>{a.plaka}</span>
            </li>
          ))}
        </ul>
        <p className="kucuk-not">Araca tıklayınca tanımları yüklenir; değiştirdiğiniz alarmlar işaretli tüm araçlara kaydedilir.</p>
      </aside>

      <section className="gecmis-icerik yonetim-icerik">
        <div className="yonetim-form">
          <h2><Bell size={20} />Bildirim ayarları</h2>
          {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          {tanim && (
            <div className="izgara-2">
              <AliciFormu tur="eposta" baslik="Alarmlar e-posta ile bildirilsin" ikon={<Mail size={16} />} veri={tanim.eposta} onMesaj={setMesaj} />
              <AliciFormu tur="sms" baslik="Alarmlar SMS ile bildirilsin" ikon={<MessageSquare size={16} />} veri={tanim.sms} onMesaj={setMesaj} />
            </div>
          )}

          {!kaynak && <div className="yapim kucuk"><Bell size={36} /><p>Tanımları görmek için soldan bir araç seçin.</p></div>}
          {tanim && kaynak && Object.keys(ayar).length > 0 && (
            <fieldset>
              <legend>Alarm tanımları · {araclar.find(a => a.id === kaynak)?.plaka}</legend>
              <table className="belge-tablo alarm-tablo">
                <thead><tr><th /><th>Durum</th><th>Değer</th><th>Saat aralığı</th><th>E-posta</th><th>SMS</th></tr></thead>
                <tbody>
                  {tanim.alarmlar.map(t => (
                    <AlarmSatiri key={t.kod} t={t} a={ayar[t.kod]} degisti={degisenler.includes(t)} tanim={tanim} onDegis={d => guncelle(t.kod, d)} />
                  ))}
                </tbody>
              </table>
              <p className="kucuk-not">Hız alarmı cihaza komut olarak gönderilir; diğerleri sunucuda izlenir. Bölge seçilmezse tüm bölgeleriniz için geçerli olur.</p>
            </fieldset>
          )}
          {kaynak && (
            <button className="ana-dugme genislik-oto" disabled={mesgul || !degisenler.length || !secili.size} onClick={kaydet}>
              {mesgul ? <span className="donen" /> : <><Save size={16} />{!degisenler.length ? 'Değişiklik yok' : `${degisenler.length} alarmı ${secili.size} araca kaydet`}</>}
            </button>
          )}
        </div>
      </section>
    </div>
  )
}

function AlarmSatiri({ t, a, degisti, tanim, onDegis }: {
  t: AlarmTanimi; a: AlarmAyari; degisti: boolean; tanim: BildirimTanimlari; onDegis: (d: Partial<AlarmAyari>) => void
}) {
  const kapali = a.durum === 0
  const sayi = (v: string) => (v.trim() === '' || isNaN(Number(v.replace(',', '.'))) ? null : Number(v.replace(',', '.')))
  return (
    <tr className={degisti ? 'degisti' : ''}>
      <th>{t.baslik}{degisti && <small className="degisti-etiket"> · değişti</small>}</th>
      <td>
        <select className="secim" value={a.durum} onChange={e => onDegis({ durum: Number(e.target.value) })}>
          {t.durumlar.map(d => <option key={d.deger} value={d.deger}>{d.ad}</option>)}
        </select>
      </td>
      <td className="alarm-deger">
        {t.sayi1 && <input className="secim kisa" inputMode="decimal" placeholder={t.sayi1} title={t.sayi1} disabled={kapali}
          value={a.sayi1 ?? ''} onChange={e => onDegis({ sayi1: sayi(e.target.value) })} />}
        {t.sayi2 && <input className="secim kisa" inputMode="decimal" placeholder={t.sayi2} title={t.sayi2} disabled={kapali}
          value={a.sayi2 ?? ''} onChange={e => onDegis({ sayi2: sayi(e.target.value) })} />}
        {t.rota && (
          <select className="secim" disabled={kapali} value={a.rota ?? ''} onChange={e => onDegis({ rota: e.target.value ? Number(e.target.value) : null })}>
            <option value="">Rota seçiniz</option>
            {tanim.rotalar.map(r => <option key={r.id} value={r.id}>{r.ad}</option>)}
          </select>
        )}
        {t.bolge && (
          <details className="bolge-secimi">
            <summary>{a.bolgeler?.length ? `${a.bolgeler.length} bölge` : 'Tüm bölgeler'}</summary>
            <div>
              {tanim.bolgeler.length === 0 && <span className="soluk">Tanımlı bölgeniz yok.</span>}
              {tanim.bolgeler.map(b => (
                <label key={b.id} className="onay-satir">
                  <input type="checkbox" checked={a.bolgeler?.includes(b.id) ?? false} onChange={e => {
                    const y = new Set(a.bolgeler ?? [])
                    if (e.target.checked) y.add(b.id); else y.delete(b.id)
                    onDegis({ bolgeler: [...y] })
                  }} />{b.ad}
                </label>
              ))}
            </div>
          </details>
        )}
      </td>
      <td>
        {t.saat && <>
          <select className="secim saat" disabled={kapali} value={a.bas ?? '-'} onChange={e => onDegis({ bas: e.target.value === '-' ? null : e.target.value })}>
            {SAATLER.map(s => <option key={s} value={s}>{s === '-' ? '-' : `${s.padStart(2, '0')}:00`}</option>)}
          </select>
          {' – '}
          <select className="secim saat" disabled={kapali} value={a.bit ?? '-'} onChange={e => onDegis({ bit: e.target.value === '-' ? null : e.target.value })}>
            {SAATLER.map(s => <option key={s} value={s}>{s === '-' ? '-' : `${s.padStart(2, '0')}:00`}</option>)}
          </select>
        </>}
      </td>
      <td><input type="checkbox" checked={a.eposta} onChange={e => onDegis({ eposta: e.target.checked })} /></td>
      <td><input type="checkbox" checked={a.sms} onChange={e => onDegis({ sms: e.target.checked })} /></td>
    </tr>
  )
}

function AliciFormu({ tur, baslik, ikon, veri, onMesaj }: {
  tur: 'eposta' | 'sms'; baslik: string; ikon: ReactNode; veri: { acik: boolean; adresler: string[] }
  onMesaj: (m: { tur: 'ok' | 'hata'; metin: string }) => void
}) {
  const [acik, setAcik] = useState(veri.acik)
  const [adresler, setAdresler] = useState(veri.adresler)
  const [mesgul, setMesgul] = useState(false)
  const kaydet = async () => {
    setMesgul(true)
    try { onMesaj({ tur: 'ok', metin: (await api.bildirimAlicilari(tur, acik, adresler)).mesaj }) }
    catch (e) { onMesaj({ tur: 'hata', metin: (e as Error).message }) } finally { setMesgul(false) }
  }
  return (
    <fieldset>
      <legend>{ikon} {tur === 'eposta' ? 'E-posta alıcıları' : 'SMS alıcıları'}</legend>
      <label className="onay-satir"><input type="checkbox" checked={acik} onChange={e => setAcik(e.target.checked)} />{baslik}</label>
      {adresler.map((v, i) => (
        <label key={i} className="alan">
          <span>{i + 1}. {tur === 'eposta' ? 'E-posta' : 'GSM (5xx xxx xx xx)'}</span>
          <input className="secim" type={tur === 'eposta' ? 'email' : 'tel'} disabled={!acik} value={v}
            onChange={e => setAdresler(a => a.map((x, j) => (j === i ? e.target.value : x)))} />
        </label>
      ))}
      <button type="button" className="ikincil-dugme" disabled={mesgul} onClick={kaydet}><Save size={15} />Kaydet</button>
    </fieldset>
  )
}
