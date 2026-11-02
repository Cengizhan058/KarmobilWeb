import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Eye, EyeOff, Lock, User, ArrowRight, Headset, Globe, CircleAlert } from 'lucide-react'
import { api, auth, type Bayi } from '../api/client.ts'
import TemaSecici from '../components/TemaSecici.tsx'

export default function Giris() {
  const navigate = useNavigate()
  const [kullaniciAdi, setKullaniciAdi] = useState('')
  const [sifre, setSifre] = useState('')
  const [sifreGoster, setSifreGoster] = useState(false)
  const [capsLock, setCapsLock] = useState(false)
  const [gonderiliyor, setGonderiliyor] = useState(false)
  const [hata, setHata] = useState<string | null>(null)
  const [bayi, setBayi] = useState<Bayi | null>(null)

  useEffect(() => {
    if (auth.isLoggedIn()) navigate('/', { replace: true })
    api.bayi().then(setBayi).catch(() => { /* destek satırı gösterilmez */ })
  }, [navigate])

  const gonder = async (e: FormEvent) => {
    e.preventDefault()
    setHata(null)
    setGonderiliyor(true)
    try {
      const { token } = await api.giris(kullaniciAdi, sifre)
      auth.kaydet(token)
      navigate('/', { replace: true })
    } catch (err) {
      setHata(err instanceof Error ? err.message : 'Giriş yapılamadı.')
    } finally {
      setGonderiliyor(false)
    }
  }

  const bos = (v: string | null | undefined) => !v || v.trim() === '' || v.trim() === '-'

  return (
    <div className="giris">
      <section className="giris-tanitim">
        <div className="giris-ust">Karmobil Araç Takip Sistemleri</div>
        <div className="giris-orta">
          <h1>Filonuzu tek ekrandan, <span>anlık</span> yönetin.</h1>
          <p>Konum, rota geçmişi, alarm ve gider bilgileri bir arada.</p>
        </div>
        <div className="giris-alt">© {new Date().getFullYear()} Karmobil Araç Takip Sistemleri Ltd. Şti.</div>
      </section>

      <section className="giris-panel">
        <div className="giris-tema"><TemaSecici /></div>
        <form className="giris-kart" onSubmit={gonder}>
          <img className="giris-logo" src="/logo.gif" alt="Karmobil" />
          <h2>Giriş yap</h2>
          <p className="giris-aciklama">Hesap bilgilerinizle devam edin.</p>

          {hata && <div className="uyari-kutu"><CircleAlert size={16} />{hata}</div>}

          <label className="alan">
            <span>Kullanıcı adı</span>
            <div className="girdi">
              <User size={16} />
              <input value={kullaniciAdi} onChange={e => setKullaniciAdi(e.target.value)} autoComplete="username" autoFocus required />
            </div>
          </label>

          <label className="alan">
            <span>Şifre</span>
            <div className="girdi">
              <Lock size={16} />
              <input
                type={sifreGoster ? 'text' : 'password'}
                value={sifre}
                onChange={e => setSifre(e.target.value)}
                onKeyUp={e => setCapsLock(e.getModifierState('CapsLock'))}
                autoComplete="current-password"
                required
              />
              <button type="button" className="girdi-dugme" title={sifreGoster ? 'Şifreyi gizle' : 'Şifreyi göster'} onClick={() => setSifreGoster(s => !s)}>
                {sifreGoster ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            {capsLock && <small className="caps">Caps Lock açık</small>}
          </label>

          <button className="ana-dugme" type="submit" disabled={gonderiliyor}>
            {gonderiliyor ? <span className="donen" /> : <>Giriş Yap <ArrowRight size={18} /></>}
          </button>

          {bayi && (!bos(bayi.destekTel) || !bos(bayi.destekWeb)) && (
            <div className="destek">
              {!bos(bayi.destekTel) && <a href={`tel:${bayi.destekTel}`}><Headset size={14} />{bayi.destekTel}</a>}
              {!bos(bayi.destekWeb) && <span><Globe size={14} />{bayi.destekWeb}</span>}
            </div>
          )}
        </form>
      </section>
    </div>
  )
}
