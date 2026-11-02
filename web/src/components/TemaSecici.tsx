import { useEffect, useRef, useState } from 'react'
import { Check, Palette } from 'lucide-react'
import { TEMALAR, aktifTema, temaUygula, type TemaId } from '../lib/tema.ts'

export default function TemaSecici() {
  const [acik, setAcik] = useState(false)
  const [tema, setTema] = useState<TemaId>(aktifTema())
  const kutu = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!acik) return
    const kapat = (e: MouseEvent) => {
      if (kutu.current && !kutu.current.contains(e.target as Node)) setAcik(false)
    }
    document.addEventListener('mousedown', kapat)
    return () => document.removeEventListener('mousedown', kapat)
  }, [acik])

  const sec = (t: TemaId) => {
    temaUygula(t)
    setTema(t)
    setAcik(false)
  }

  return (
    <div className="tema-secici" ref={kutu}>
      <button className="ikon-dugme" title="Tema rengi" onClick={() => setAcik(a => !a)}>
        <Palette size={18} />
      </button>
      {acik && (
        <div className="tema-liste">
          <div className="tema-baslik">Tema rengi</div>
          {TEMALAR.map(t => (
            <button key={t.id} className={'tema-madde' + (t.id === tema ? ' secili' : '')} onClick={() => sec(t.id)}>
              <span className="tema-ornek" style={{ background: t.ornek }} />
              {t.ad}
              {t.id === tema && <Check size={14} className="tema-tik" />}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
