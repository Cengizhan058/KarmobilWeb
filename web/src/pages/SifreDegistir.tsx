import { useState, type FormEvent } from 'react'
import { KeyRound, Save } from 'lucide-react'
import { api } from '../api/client.ts'

/** Ayarlar → Şifre / Parola Ayarları (eski pwd_settings, _prcSetUserPwd.asp). Mevcut şifre doğruysa güncellenir. */
export default function SifreDegistir() {
  const [f, setF] = useState({ mevcut: '', yeni: '', yeni2: '' })
  const [mesaj, setMesaj] = useState<{ tur: 'ok' | 'hata'; metin: string } | null>(null)
  const [mesgul, setMesgul] = useState(false)

  const kaydet = async (e: FormEvent) => {
    e.preventDefault()
    if (f.yeni !== f.yeni2) { setMesaj({ tur: 'hata', metin: 'Yeni şifreler aynı değil.' }); return }
    setMesgul(true); setMesaj(null)
    try {
      setMesaj({ tur: 'ok', metin: (await api.sifreDegistir(f.mevcut, f.yeni)).mesaj })
      setF({ mevcut: '', yeni: '', yeni2: '' })
    } catch (err) { setMesaj({ tur: 'hata', metin: (err as Error).message }) } finally { setMesgul(false) }
  }

  return (
    <div className="tek-sayfa">
      <div className="islem-baslik"><h2><KeyRound size={20} /> Şifre değiştir</h2></div>
      {mesaj && <div className={mesaj.tur === 'ok' ? 'bilgi-kutu' : 'uyari-kutu'}>{mesaj.metin}</div>}
      <form className="gosterge-kart sifre-kart" onSubmit={kaydet}>
        <label className="alan"><span>Mevcut şifre</span>
          <input className="secim" type="password" autoComplete="current-password" required value={f.mevcut} onChange={e => setF({ ...f, mevcut: e.target.value })} />
        </label>
        <label className="alan"><span>Yeni şifre (en az 6 karakter)</span>
          <input className="secim" type="password" autoComplete="new-password" required minLength={6} value={f.yeni} onChange={e => setF({ ...f, yeni: e.target.value })} />
        </label>
        <label className="alan"><span>Yeni şifre (tekrar)</span>
          <input className="secim" type="password" autoComplete="new-password" required minLength={6} value={f.yeni2} onChange={e => setF({ ...f, yeni2: e.target.value })} />
        </label>
        <p className="kucuk-not soluk">Yeni şifre eski sitede ve mobil uygulamada da geçerli olur.</p>
        <button className="ana-dugme genislik-oto" disabled={mesgul}>{mesgul ? <span className="donen" /> : <><Save size={16} />Kaydet</>}</button>
      </form>
    </div>
  )
}
