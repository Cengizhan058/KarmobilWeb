import { useState } from 'react'
import { Cpu, Send } from 'lucide-react'
import { api, type CihazProgrami } from '../api/client.ts'
import { tamZaman } from '../lib/arac.ts'
import CokluAracListesi from '../components/CokluAracListesi.tsx'

/**
 * Ayarlar → Cihaz Programlama (eski device_settings): veri gönderim periyotları ve hız limiti.
 * Ayarlar cihaza komut olarak gider (gateway iletir); yalnız değiştirilenler gönderilir.
 */
export default function CihazProgramlama() {
  const [secili, setSecili] = useState<Set<number>>(new Set())
  const [kaynak, setKaynak] = useState<number | null>(null)
  const [ilk, setIlk] = useState<CihazProgrami[]>([])
  const [ayar, setAyar] = useState<CihazProgrami[]>([])
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  const yukle = (id: number) => {
    setKaynak(id); setMesaj(null)
    api.cihazProgrami(id).then(p => { setIlk(p); setAyar(p) }).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }

  const degisti = (a: CihazProgrami, i: number) => a.durum !== ilk[i]?.durum || a.deger !== ilk[i]?.deger
  const degisenler = ayar.filter(degisti)

  const gonder = async () => {
    if (!degisenler.length || !secili.size) return
    if (!confirm(`${degisenler.map(d => d.baslik).join(', ')} ayarı ${secili.size} aracın cihazına gönderilecek. Onaylıyor musunuz?`)) return
    setMesgul(true); setMesaj(null)
    try {
      const r = await api.cihazProgramGonder([...secili], degisenler.map(d => ({ kod: d.kod, durum: d.durum, deger: d.deger })))
      setMesaj({ tur: 'ok', metin: r.mesaj })
      if (kaynak) yukle(kaynak)
    } catch (e) { setMesaj({ tur: 'hata', metin: (e as Error).message }) } finally { setMesgul(false) }
  }

  const guncelle = (kod: string, d: Partial<CihazProgrami>) => setAyar(a => a.map(x => (x.kod === kod ? { ...x, ...d } : x)))

  return (
    <div className="gecmis-sayfa">
      <CokluAracListesi kaynak={kaynak} secili={secili} onKaynak={yukle} onSecili={setSecili}
        not="Araca tıklayınca mevcut ayarları yüklenir; değiştirdiğiniz ayarlar işaretli tüm araçların cihazına gönderilir." />
      <section className="gecmis-icerik yonetim-icerik">
        <div className="yonetim-form">
          <h2><Cpu size={20} />Cihaz programlama</h2>
          {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          {!kaynak && <div className="yapim kucuk"><Cpu size={36} /><p>Soldan bir araç seçin.</p></div>}
          {kaynak && ayar.length > 0 && (
            <fieldset>
              <legend>Cihaz ayarları</legend>
              <table className="belge-tablo alarm-tablo">
                <thead><tr><th /><th>Durum</th><th>Değer</th><th>Son gönderim</th></tr></thead>
                <tbody>
                  {ayar.map((a, i) => (
                    <tr key={a.kod} className={degisti(a, i) ? 'degisti' : ''}>
                      <th>{a.baslik}{degisti(a, i) && <small className="degisti-etiket"> · değişti</small>}</th>
                      <td>
                        <select className="secim" value={a.durum} onChange={e => guncelle(a.kod, { durum: Number(e.target.value) })}>
                          <option value={0}>Kapalı</option><option value={1}>Açık</option>
                        </select>
                      </td>
                      <td className="alarm-deger">
                        <input className="secim kisa" inputMode="decimal" value={a.deger ?? ''} placeholder={`en az ${a.min}`}
                          onChange={e => guncelle(a.kod, { deger: e.target.value.trim() === '' ? null : Number(e.target.value.replace(',', '.')) })} />
                        <span className="soluk">{a.birim}</span>
                      </td>
                      <td className="soluk">{a.tarih ? <>{tamZaman(a.tarih)}{a.bekliyor && ' · cihaza iletilmeyi bekliyor'}</> : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </fieldset>
          )}
          {kaynak && (
            <button className="ana-dugme genislik-oto" disabled={mesgul || !degisenler.length || !secili.size} onClick={gonder}>
              {mesgul ? <span className="donen" /> : <><Send size={16} />{degisenler.length ? `${secili.size} araca gönder` : 'Değişiklik yok'}</>}
            </button>
          )}
        </div>
      </section>
    </div>
  )
}
