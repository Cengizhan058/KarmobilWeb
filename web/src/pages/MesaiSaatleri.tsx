import { useEffect, useMemo, useState } from 'react'
import { Clock, Save, Search } from 'lucide-react'
import { api } from '../api/client.ts'

const GUNLER = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']
const VARSAYILAN = [...Array(5).fill(['08:30', '18:30']).flat(), '-', '-', '-', '-'] as string[]

/**
 * Ayarlar → Mesai Saatleri (eski _getWorkHours / _prcWorkHours). Bir aracın saatleri yüklenir,
 * seçilen tüm araçlara birden uygulanabilir. Mesai dışı hareketler raporlarda bu saatlere göre ayrılır.
 */
export default function MesaiSaatleri() {
  // Mesai ekranı herkese açık (eski menüde de yetki koşulu yoktu); araç yönetimi yetkisi istemeyen liste kullanılır.
  const [araclar, setAraclar] = useState<{ id: number; plaka: string }[]>([])
  const [secili, setSecili] = useState<Set<number>>(new Set())
  const [kaynak, setKaynak] = useState<number | null>(null)
  const [saatler, setSaatler] = useState<string[]>(VARSAYILAN)
  const [ara, setAra] = useState('')
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  useEffect(() => { api.araclar().then(a => setAraclar([...a].sort((x, y) => x.plaka.localeCompare(y.plaka, 'tr')))).catch(e => setMesaj({ tur: 'hata', metin: e.message })) }, [])

  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q)) : araclar
  }, [araclar, ara])

  const yukle = (id: number) => {
    setKaynak(id); setMesaj(null)
    setSecili(s => new Set(s).add(id))
    api.mesai(id).then(setSaatler).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }
  const isaretle = (id: number, v: boolean) => setSecili(s => { const y = new Set(s); if (v) y.add(id); else y.delete(id); return y })

  const gunAyarla = (gun: number, bas: string, bit: string) => setSaatler(s => { const y = [...s]; y[gun * 2] = bas; y[gun * 2 + 1] = bit; return y })

  const kaydet = async () => {
    setMesgul(true); setMesaj(null)
    try {
      setMesaj({ tur: 'ok', metin: (await api.mesaiKaydet([...secili], saatler)).mesaj })
    } catch (e) {
      setMesaj({ tur: 'hata', metin: e instanceof Error ? e.message : 'Kaydedilemedi.' })
    } finally {
      setMesgul(false)
    }
  }

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
              <input type="checkbox" checked={secili.has(a.id)} onClick={e => e.stopPropagation()} onChange={e => isaretle(a.id, e.target.checked)} />
              <span>{a.plaka}</span>
            </li>
          ))}
        </ul>
        <p className="kucuk-not">Araca tıklayınca saatleri yüklenir; işaretli tüm araçlara birden kaydedilir.</p>
      </aside>

      <section className="gecmis-icerik yonetim-icerik">
        <div className="yonetim-form">
          <h2><Clock size={20} />Mesai saatleri</h2>
          {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          <fieldset>
            <table className="belge-tablo">
              <thead><tr><th /><th>Mesai var</th><th>Başlangıç</th><th>Bitiş</th></tr></thead>
              <tbody>
                {GUNLER.map((g, i) => {
                  const bas = saatler[i * 2], bit = saatler[i * 2 + 1]
                  const var_ = bas !== '-' && bit !== '-'
                  return (
                    <tr key={g}>
                      <th>{g}</th>
                      <td><input type="checkbox" checked={var_} onChange={e => gunAyarla(i, e.target.checked ? '08:30' : '-', e.target.checked ? '18:30' : '-')} /></td>
                      <td><input className="secim" type="time" disabled={!var_} value={var_ ? bas : ''} onChange={e => gunAyarla(i, e.target.value || '-', bit)} /></td>
                      <td><input className="secim" type="time" disabled={!var_} value={var_ ? bit : ''} onChange={e => gunAyarla(i, bas, e.target.value || '-')} /></td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </fieldset>
          <button className="ana-dugme genislik-oto" disabled={mesgul || secili.size === 0} onClick={kaydet}>
            {mesgul ? <span className="donen" /> : <><Save size={16} />{secili.size ? `${secili.size} araca kaydet` : 'Araç seçiniz'}</>}
          </button>
        </div>
      </section>
    </div>
  )
}
