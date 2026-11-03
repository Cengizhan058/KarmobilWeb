import { useState } from 'react'
import { Ban, Save } from 'lucide-react'
import { api } from '../api/client.ts'
import CokluAracListesi from '../components/CokluAracListesi.tsx'

const GUNLER = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']
type Gun = { bas: string | null; bit: string | null }

/** Ayarlar → Akıllı Motor Blokaj (eski smart_block): günlere göre motorun kilitli tutulacağı saat aralığı. */
export default function AkilliBlokaj() {
  const [secili, setSecili] = useState<Set<number>>(new Set())
  const [kaynak, setKaynak] = useState<number | null>(null)
  const [gunler, setGunler] = useState<Gun[]>([])
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  const yukle = (id: number) => {
    setKaynak(id); setMesaj(null)
    api.akilliBlokaj(id).then(setGunler).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }

  const kaydet = async () => {
    if (!secili.size) return
    if (!confirm(`Motor blokaj planı ${secili.size} araca kaydedilecek. Bu saatlerde motor kilitlenir. Onaylıyor musunuz?`)) return
    setMesgul(true); setMesaj(null)
    try { setMesaj({ tur: 'ok', metin: (await api.akilliBlokajKaydet([...secili], gunler)).mesaj }) }
    catch (e) { setMesaj({ tur: 'hata', metin: (e as Error).message }) } finally { setMesgul(false) }
  }

  const ayarla = (i: number, g: Gun) => setGunler(l => l.map((x, j) => (j === i ? g : x)))

  return (
    <div className="gecmis-sayfa">
      <CokluAracListesi kaynak={kaynak} secili={secili} onKaynak={yukle} onSecili={setSecili}
        not="Araca tıklayınca planı yüklenir; işaretli tüm araçlara birden kaydedilir." />
      <section className="gecmis-icerik yonetim-icerik">
        <div className="yonetim-form">
          <h2><Ban size={20} />Akıllı motor blokaj</h2>
          {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          {!kaynak && <div className="yapim kucuk"><Ban size={36} /><p>Soldan bir araç seçin.</p></div>}
          {kaynak && gunler.length === 7 && (
            <fieldset>
              <legend>Blokaj saatleri</legend>
              <table className="belge-tablo">
                <thead><tr><th /><th>Blokaj</th><th>Başlangıç</th><th>Bitiş</th></tr></thead>
                <tbody>
                  {GUNLER.map((ad, i) => {
                    const g = gunler[i], var_ = !!g.bas
                    return (
                      <tr key={ad}>
                        <th>{ad}</th>
                        <td><input type="checkbox" checked={var_} onChange={e => ayarla(i, e.target.checked ? { bas: '00:00', bit: '06:00' } : { bas: null, bit: null })} /></td>
                        <td><input className="secim" type="time" disabled={!var_} value={g.bas ?? ''} onChange={e => ayarla(i, { ...g, bas: e.target.value || null })} /></td>
                        <td><input className="secim" type="time" disabled={!var_} value={g.bit ?? ''} onChange={e => ayarla(i, { ...g, bit: e.target.value || null })} /></td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
              <p className="kucuk-not">Yalnız motor blokaj donanımı olan araçlarda çalışır.</p>
            </fieldset>
          )}
          {kaynak && (
            <button className="ana-dugme genislik-oto" disabled={mesgul || !secili.size} onClick={kaydet}>
              {mesgul ? <span className="donen" /> : <><Save size={16} />{`${secili.size} araca kaydet`}</>}
            </button>
          )}
        </div>
      </section>
    </div>
  )
}
