import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Plus, Save, Search, ShieldCheck, Trash2, UserRound } from 'lucide-react'
import { api, type AltKullaniciListesi, type AltYetkiler } from '../api/client.ts'

const YETKILER: { ad: keyof AltYetkiler; baslik: string }[] = [
  { ad: 'harita', baslik: 'Harita' },
  { ad: 'aracYonetimi', baslik: 'Araç yönetimi' },
  { ad: 'cihazProgramlama', baslik: 'Cihaz programlama' },
  { ad: 'grupYonetimi', baslik: 'Grup yönetimi' },
  { ad: 'raporlar', baslik: 'Raporlama' },
]

const bosYetki: AltYetkiler = { harita: true, aracYonetimi: false, cihazProgramlama: false, grupYonetimi: false, raporlar: true }

type Form = { id: number | null; kullaniciAdi: string; sifre: string; sifre2: string; yetkiler: AltYetkiler; araclar: Set<number> }

/** Ayarlar → Kullanıcı Yetkilendirme (eski sub_users): alt kullanıcı hesapları, yetkileri ve görebileceği araçlar. */
export default function AltKullanicilar() {
  const [veri, setVeri] = useState<AltKullaniciListesi | null>(null)
  const [araclar, setAraclar] = useState<{ id: number; plaka: string }[]>([])
  const [form, setForm] = useState<Form | null>(null)
  const [ara, setAra] = useState('')
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  const yenile = () => api.altKullanicilar().then(setVeri).catch(e => setMesaj({ tur: 'hata', metin: e.message }))
  useEffect(() => {
    yenile()
    api.araclar().then(a => setAraclar([...a].sort((x, y) => x.plaka.localeCompare(y.plaka, 'tr')))).catch(() => { /* boş liste */ })
  }, [])

  const liste = useMemo(() => {
    const q = ara.trim().toLocaleUpperCase('tr-TR')
    return q ? araclar.filter(a => a.plaka.toLocaleUpperCase('tr-TR').includes(q)) : araclar
  }, [araclar, ara])

  const sec = async (id: number) => {
    const k = veri?.kullanicilar.find(x => x.id === id)
    if (!k) return
    setMesaj(null)
    try {
      const a = await api.altKullaniciAraclari(id)
      setForm({ id, kullaniciAdi: k.kullaniciAdi, sifre: '', sifre2: '', yetkiler: k.yetkiler, araclar: new Set(a) })
    } catch (e) { setMesaj({ tur: 'hata', metin: (e as Error).message }) }
  }

  const kaydet = async (e: FormEvent) => {
    e.preventDefault()
    if (!form) return
    if (form.sifre !== form.sifre2) { setMesaj({ tur: 'hata', metin: 'Şifreler aynı değil.' }); return }
    setMesgul(true); setMesaj(null)
    try {
      const r = await api.altKullaniciKaydet(form.id, {
        kullaniciAdi: form.kullaniciAdi, sifre: form.sifre || null, yetkiler: form.yetkiler, araclar: [...form.araclar],
      })
      setMesaj({ tur: 'ok', metin: r.mesaj })
      await yenile()
      setForm(f => f && { ...f, id: f.id ?? r.id ?? null, sifre: '', sifre2: '' })
    } catch (err) { setMesaj({ tur: 'hata', metin: (err as Error).message }) } finally { setMesgul(false) }
  }

  const sil = async () => {
    if (!form?.id || !confirm(`${form.kullaniciAdi} kullanıcısı silinecek. Onaylıyor musunuz?`)) return
    try {
      const r = await api.altKullaniciSil(form.id)
      setMesaj({ tur: 'ok', metin: r.mesaj }); setForm(null); yenile()
    } catch (e) { setMesaj({ tur: 'hata', metin: (e as Error).message }) }
  }

  const doluluk = veri ? `${veri.kullanicilar.length} / ${veri.limit}` : ''
  const yeniOlmaz = veri ? veri.kullanicilar.length >= veri.limit : true

  return (
    <div className="gecmis-sayfa">
      <aside className="gecmis-form yonetim-liste">
        <h3>Alt kullanıcılar <small className="soluk">{doluluk}</small></h3>
        <button className="ikincil-dugme tam" disabled={yeniOlmaz} title={yeniOlmaz ? 'Alt kullanıcı sınırına ulaşıldı.' : undefined}
          onClick={() => { setMesaj(null); setForm({ id: null, kullaniciAdi: '', sifre: '', sifre2: '', yetkiler: bosYetki, araclar: new Set() }) }}>
          <Plus size={15} />Yeni kullanıcı
        </button>
        <ul className="islem-liste uzun">
          {veri?.kullanicilar.map(k => (
            <li key={k.id} className={form?.id === k.id ? 'secili' : ''} onClick={() => sec(k.id)}>
              <UserRound size={16} /><span>{k.kullaniciAdi}</span><small>{k.aracSayisi} araç</small>
            </li>
          ))}
          {veri && !veri.kullanicilar.length && <li className="bos">Alt kullanıcı yok.</li>}
        </ul>
      </aside>

      <section className="gecmis-icerik yonetim-icerik">
        <div className="yonetim-form">
          <h2><ShieldCheck size={20} />Kullanıcı yetkilendirme</h2>
          {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
          {!form && <div className="yapim kucuk"><UserRound size={36} /><p>Soldan bir kullanıcı seçin ya da yeni kullanıcı oluşturun.</p></div>}
          {form && veri && (
            <form onSubmit={kaydet}>
              <fieldset>
                <legend>{form.id ? 'Kullanıcı bilgileri' : 'Yeni kullanıcı'}</legend>
                <div className="izgara-4">
                  <label className="alan"><span>Kullanıcı adı *</span>
                    <input className="secim" required minLength={3} autoComplete="off" value={form.kullaniciAdi} onChange={e => setForm({ ...form, kullaniciAdi: e.target.value })} />
                  </label>
                  <label className="alan"><span>Şifre{form.id ? ' (değişmeyecekse boş)' : ' *'}</span>
                    <input className="secim" type="password" autoComplete="new-password" required={!form.id} minLength={6} value={form.sifre} onChange={e => setForm({ ...form, sifre: e.target.value })} />
                  </label>
                  <label className="alan"><span>Şifre (tekrar)</span>
                    <input className="secim" type="password" autoComplete="new-password" required={!form.id || !!form.sifre} value={form.sifre2} onChange={e => setForm({ ...form, sifre2: e.target.value })} />
                  </label>
                </div>
              </fieldset>
              <fieldset>
                <legend>Yetkiler</legend>
                <div className="izgara-4">
                  {YETKILER.map(y => (
                    <label key={y.ad} className="onay-satir" title={veri.verilebilir[y.ad] ? undefined : 'Sizin hesabınızda bu yetki yok.'}>
                      <input type="checkbox" disabled={!veri.verilebilir[y.ad]} checked={form.yetkiler[y.ad] && veri.verilebilir[y.ad]}
                        onChange={e => setForm({ ...form, yetkiler: { ...form.yetkiler, [y.ad]: e.target.checked } })} />{y.baslik}
                    </label>
                  ))}
                </div>
              </fieldset>
              <fieldset>
                <legend>Görebileceği araçlar ({form.araclar.size})</legend>
                <div className="grup-arac-ust">
                  <label className="arama-kutu"><Search size={15} /><input placeholder="Plaka ara" value={ara} onChange={e => setAra(e.target.value)} /></label>
                  <button type="button" className="ikincil-dugme" onClick={() => setForm({ ...form, araclar: new Set([...form.araclar, ...liste.map(a => a.id)]) })}>Tümünü seç</button>
                  <button type="button" className="ikincil-dugme" onClick={() => setForm({ ...form, araclar: new Set() })}>Temizle</button>
                </div>
                <div className="grup-araclar">
                  {liste.map(a => (
                    <label key={a.id} className="onay-satir">
                      <input type="checkbox" checked={form.araclar.has(a.id)} onChange={e => {
                        const y = new Set(form.araclar)
                        if (e.target.checked) y.add(a.id); else y.delete(a.id)
                        setForm({ ...form, araclar: y })
                      }} />{a.plaka}
                    </label>
                  ))}
                </div>
              </fieldset>
              <div className="grup-arac-ust">
                <button className="ana-dugme genislik-oto" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={16} />Kaydet</>}</button>
                {form.id && <button type="button" className="ikincil-dugme tehlike" onClick={sil}><Trash2 size={15} />Kullanıcıyı sil</button>}
              </div>
            </form>
          )}
        </div>
      </section>
    </div>
  )
}
