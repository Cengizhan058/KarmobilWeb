import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useParams, useSearchParams } from 'react-router-dom'
import { FileSpreadsheet, Pencil, Plus, Save, Trash2, Wallet, X } from 'lucide-react'
import { api, type GiderAlani, type GiderKaydi, type GiderTuru, type YonetimAraci } from '../api/client.ts'
import AracSecimListesi from '../components/AracSecimListesi.tsx'
import { csvIndir } from '../lib/disaAktar.ts'

const para = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const sayi = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 })

function goster(a: GiderAlani, v: string | number | null | undefined): string {
  if (v === null || v === undefined || v === '') return ''
  const s = String(v)
  if (a.tip === 'tarih') return s.slice(0, 10).split('-').reverse().join('.')
  if (a.tip === 'tarihSaat') return `${s.slice(0, 10).split('-').reverse().join('.')} ${s.slice(11, 16)}`
  if (a.tip === 'tutar') return para.format(Number(v))
  if (a.tip === 'sayi' || a.tip === 'tamSayi') return sayi.format(Number(v))
  return s
}

// Sigorta bitişi: poliçe tarihi + süre (ay); eski _gridSigorta.asp'deki "Bitiş Tarihi" sütunu.
function sigortaBitis(k: GiderKaydi): string {
  const t = k.alanlar.TARIH, ay = Number(k.alanlar.SURESI)
  if (!t || !ay) return ''
  const d = new Date(`${String(t).slice(0, 10)}T00:00:00`)
  d.setMonth(d.getMonth() + ay)
  return d.toLocaleDateString('tr-TR')
}

const bosForm = (t: GiderTuru): Record<string, string> => {
  const simdi = new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString()
  return Object.fromEntries(t.alanlar.map(a => [a.kolon, a.tip === 'tarih' ? simdi.slice(0, 10) : a.tip === 'tarihSaat' ? simdi.slice(0, 16) : '']))
}

/** Gider Yönetimi → yakıt, sigorta, muayene, bakım, lastik, ceza, kaza, MTV, OGS/HGS, genel masraf (/gider/:tur). */
export default function Giderler() {
  const { tur = '' } = useParams()
  const [params, setParams] = useSearchParams()
  const seciliId = Number(params.get('arac')) || null
  const [turler, setTurler] = useState<GiderTuru[] | null>(null)
  const [araclar, setAraclar] = useState<YonetimAraci[]>([])
  const [kayitlar, setKayitlar] = useState<GiderKaydi[] | null>(null)
  const [form, setForm] = useState<{ id: number | null; alanlar: Record<string, string> } | null>(null)
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  useEffect(() => {
    api.giderTurleri().then(setTurler).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
    api.giderAraclari().then(setAraclar).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }, [])

  const t = turler?.find(x => x.kod === tur)
  const aracPlaka = araclar.find(a => a.id === seciliId)?.plaka

  const yukle = () => {
    if (!t || !seciliId) return
    setKayitlar(null)
    api.giderler(t.kod, seciliId).then(setKayitlar).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  }
  useEffect(() => { setForm(null); setMesaj(null); yukle() }, [t?.kod, seciliId]) // eslint-disable-line react-hooks/exhaustive-deps

  const toplam = useMemo(() => (kayitlar ?? []).reduce((s, k) => s + (Number(k.alanlar.FIYAT) || 0), 0), [kayitlar])
  // Listede not sütunu gösterilmez (uzun metin); Excel'e girer.
  const kolonlar = t?.alanlar.filter(a => a.tip !== 'not') ?? []

  const kaydet = async (e: FormEvent) => {
    e.preventDefault()
    if (!t || !seciliId || !form) return
    setMesgul(true); setMesaj(null)
    try {
      const r = await api.giderKaydet(t.kod, form.id, seciliId, form.alanlar)
      setMesaj({ tur: 'ok', metin: r.mesaj }); setForm(null); yukle()
    } catch (err) { setMesaj({ tur: 'hata', metin: (err as Error).message }) }
    finally { setMesgul(false) }
  }

  const sil = async (k: GiderKaydi) => {
    if (!t || !confirm(`${goster(t.alanlar[0], k.alanlar[t.alanlar[0].kolon])} tarihli kayıt silinsin mi?`)) return
    try { const r = await api.giderSil(t.kod, k.id); setMesaj({ tur: 'ok', metin: r.mesaj }); yukle() }
    catch (err) { setMesaj({ tur: 'hata', metin: (err as Error).message }) }
  }

  const duzenle = (k: GiderKaydi) => {
    if (!t) return
    setMesaj(null)
    setForm({ id: k.id, alanlar: Object.fromEntries(t.alanlar.map(a => [a.kolon, k.alanlar[a.kolon] === null ? '' : String(k.alanlar[a.kolon])])) })
  }

  const excel = () => t && kayitlar && csvIndir(`${t.kod}-${aracPlaka ?? ''}.csv`,
    [...t.alanlar.map(a => a.baslik), ...(t.kod === 'sigorta' ? ['Bitiş tarihi'] : [])],
    kayitlar.map(k => [...t.alanlar.map(a => goster(a, k.alanlar[a.kolon])), ...(t.kod === 'sigorta' ? [sigortaBitis(k)] : [])]))

  if (turler && !t) return <div className="tek-sayfa"><div className="uyari-kutu">Gider türü bulunamadı.</div></div>

  return (
    <div className="gecmis-sayfa">
      <AracSecimListesi baslik="Gider takibi açık araçlar" araclar={araclar} seciliId={seciliId} onSec={id => setParams({ arac: String(id) })} />

      <section className="gecmis-icerik yonetim-icerik">
        <div className="yonetim-form">
          <div className="islem-baslik">
            <h2><Wallet size={20} />{t?.baslik ?? 'Gider yönetimi'}{aracPlaka && <span className="soluk"> · {aracPlaka}</span>}</h2>
            {seciliId && t && (
              <div className="grup-arac-ust">
                {kayitlar && kayitlar.length > 0 && <button className="ikincil-dugme" onClick={excel}><FileSpreadsheet size={15} />Excel</button>}
                <button className="ana-dugme kucuk" onClick={() => { setMesaj(null); setForm({ id: null, alanlar: bosForm(t) }) }}><Plus size={15} />Yeni kayıt</button>
              </div>
            )}
          </div>
          {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          {!seciliId && <div className="yapim kucuk"><Wallet size={36} /><p>{araclar.length ? 'Soldan bir araç seçin.' : 'Gider takibi açık aracınız yok.'}</p></div>}

          {form && t && (
            <form onSubmit={kaydet}>
              <fieldset>
                <legend>{form.id ? 'Kaydı düzenle' : 'Yeni kayıt'}</legend>
                <div className="izgara-4">
                  {t.alanlar.map(a => (
                    <label key={a.kolon} className={`alan${a.tip === 'not' ? ' tam-satir' : ''}`}>
                      <span>{a.baslik}{a.zorunlu && ' *'}</span>
                      <GiderGirdisi alan={a} deger={form.alanlar[a.kolon] ?? ''} onDegis={v => setForm(f => f && { ...f, alanlar: { ...f.alanlar, [a.kolon]: v } })} />
                    </label>
                  ))}
                </div>
                <div className="grup-arac-ust">
                  <button className="ana-dugme kucuk" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={15} />Kaydet</>}</button>
                  <button type="button" className="ikincil-dugme" onClick={() => setForm(null)}><X size={15} />Vazgeç</button>
                </div>
              </fieldset>
            </form>
          )}

          {seciliId && !kayitlar && <div className="yapim kucuk"><span className="donen" /></div>}
          {kayitlar && kayitlar.length === 0 && !form && <div className="bilgi-kutu">Bu araç için kayıt yok.</div>}
          {kayitlar && kayitlar.length > 0 && t && (
            <table className="tablo">
              <thead>
                <tr>
                  {kolonlar.map(a => <th key={a.kolon}>{a.baslik}</th>)}
                  {t.kod === 'sigorta' && <th>Bitiş tarihi</th>}
                  <th />
                </tr>
              </thead>
              <tbody>
                {kayitlar.map(k => (
                  <tr key={k.id} title={String(k.alanlar.ACIKLAMA ?? '')}>
                    {kolonlar.map(a => <td key={a.kolon} className={a.tip === 'tutar' || a.tip === 'sayi' || a.tip === 'tamSayi' ? 'sag' : ''}>{goster(a, k.alanlar[a.kolon])}</td>)}
                    {t.kod === 'sigorta' && <td>{sigortaBitis(k)}</td>}
                    <td className="satir-dugmeler">
                      <button title="Düzenle" onClick={() => duzenle(k)}><Pencil size={15} /></button>
                      <button title="Sil" onClick={() => sil(k)}><Trash2 size={15} /></button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  {kolonlar.map((a, i) => <td key={a.kolon} className={a.tip === 'tutar' ? 'sag' : ''}>{i === 0 ? <strong>Toplam ({kayitlar.length})</strong> : a.kolon === 'FIYAT' ? <strong>{para.format(toplam)}</strong> : ''}</td>)}
                  {t.kod === 'sigorta' && <td />}
                  <td />
                </tr>
              </tfoot>
            </table>
          )}
        </div>
      </section>
    </div>
  )
}

function GiderGirdisi({ alan: a, deger, onDegis }: { alan: GiderAlani; deger: string; onDegis: (v: string) => void }) {
  switch (a.tip) {
    case 'tarih': return <input className="secim" type="date" required={a.zorunlu} value={deger} onChange={e => onDegis(e.target.value)} />
    case 'tarihSaat': return <input className="secim" type="datetime-local" required={a.zorunlu} value={deger} onChange={e => onDegis(e.target.value)} />
    case 'secim':
      return (
        <select className="secim" value={deger} onChange={e => onDegis(e.target.value)}>
          <option value="">Seçiniz</option>
          {a.secenekler?.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
      )
    case 'not': return <textarea className="secim" rows={3} maxLength={a.uz} value={deger} onChange={e => onDegis(e.target.value)} />
    case 'tamSayi': case 'sayi': case 'tutar':
      return <input className="secim" inputMode="decimal" value={deger} onChange={e => onDegis(e.target.value.replace(/[^0-9.,]/g, ''))} />
    default: return <input className="secim" maxLength={a.uz} value={deger} onChange={e => onDegis(e.target.value)} />
  }
}
