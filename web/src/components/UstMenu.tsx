import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check, ChevronDown, LogOut } from 'lucide-react'
import { auth, type GorunumAyari, type OturumKullanici } from '../api/client.ts'
import { gorunurMenu, type MenuMaddesi } from '../lib/menu.ts'
import TemaSecici from './TemaSecici.tsx'

const ETIKETLER = ['Plaka', 'Cihaz ID', 'Sürücü', 'Hız']

export default function UstMenu({ kullanici, onGorunum }: { kullanici: OturumKullanici; onGorunum: (a: GorunumAyari) => void }) {
  const navigate = useNavigate()
  const [acikId, setAcikId] = useState<string | null>(null)
  const cubuk = useRef<HTMLElement>(null)
  const menu = gorunurMenu(kullanici)

  useEffect(() => {
    if (!acikId) return
    const kapat = (e: MouseEvent) => {
      if (cubuk.current && !cubuk.current.contains(e.target as Node)) setAcikId(null)
    }
    document.addEventListener('mousedown', kapat)
    return () => document.removeEventListener('mousedown', kapat)
  }, [acikId])

  const git = (m: MenuMaddesi) => {
    setAcikId(null)
    navigate(m.yol ?? `/yapim/${m.id}?ad=${encodeURIComponent(m.ad)}`)
  }

  const cikis = () => {
    auth.cikis()
    navigate('/giris', { replace: true })
  }

  // ADI_SOYADI çoğu kayıtta boş ya da "-"; o durumda kullanıcı adı gösterilir.
  const adSoyad = kullanici.adSoyad?.trim()
  const ad = adSoyad && adSoyad !== '-' ? adSoyad : kullanici.kullaniciAdi
  const basHarf = ad.slice(0, 2).toLocaleUpperCase('tr-TR')

  return (
    <header className="ust-cubuk" ref={cubuk}>
      <div className="ust-logo" onClick={() => navigate('/')}>
        <img src="/logo.gif" alt="Karmobil" />
      </div>

      <nav className="ust-menu">
        {menu.map(m => {
          const Ikon = m.ikon
          const acik = acikId === m.id
          return (
            <div key={m.id} className="menu-grup">
              <button
                className={'menu-dugme' + (acik ? ' acik' : '')}
                onClick={() => (m.alt?.length ? setAcikId(acik ? null : m.id) : git(m))}
                onMouseEnter={() => acikId && m.alt?.length && setAcikId(m.id)}
              >
                <Ikon size={17} />
                <span>{m.ad}</span>
                {m.alt?.length ? <ChevronDown size={14} className="menu-ok" /> : null}
              </button>
              {acik && m.alt && (
                <div className="alt-menu">
                  {m.alt.map(a => {
                    const AltIkon = a.ikon
                    if (a.ayar === 'etiket') return (
                      <div key={a.id} className="alt-grup">
                        <div className="alt-grup-baslik"><AltIkon size={16} />{a.ad}</div>
                        {ETIKETLER.map((e, i) => (
                          <button key={e} className="alt-madde secenek" onClick={() => onGorunum({ etiket: i })}>
                            <span className={'radyo' + (kullanici.etiket === i ? ' secili' : '')} />{e}
                          </button>
                        ))}
                      </div>
                    )
                    if (a.ayar === 'grupla' || a.ayar === 'arizali') {
                      const deger = a.ayar === 'grupla' ? kullanici.araclariGrupla : kullanici.arizalilariGizle
                      return (
                        <button key={a.id} className="alt-madde" onClick={() => onGorunum(a.ayar === 'grupla' ? { araclariGrupla: !deger } : { arizalilariGizle: !deger })}>
                          <span className={'onay' + (deger ? ' secili' : '')}>{deger && <Check size={12} />}</span>
                          <span>{a.ad}</span>
                        </button>
                      )
                    }
                    return (
                      <button key={a.id} className="alt-madde" onClick={() => git(a)}>
                        <AltIkon size={16} />
                        <span>{a.ad}</span>
                      </button>
                    )
                  })}
                </div>
              )}
            </div>
          )
        })}
      </nav>

      <div className="ust-kullanici">
        <TemaSecici />
        <span className="avatar">{basHarf}</span>
        <span className="kullanici-ad">
          <strong>{ad}</strong>
          <small>{kullanici.seviye === 'a' ? 'Yönetici' : 'Kullanıcı'}</small>
        </span>
        <button className="ikon-dugme cikis" title="Çıkış yap" onClick={cikis}>
          <LogOut size={18} />
        </button>
      </div>
    </header>
  )
}
