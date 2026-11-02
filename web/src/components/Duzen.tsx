import { useEffect, useState } from 'react'
import { Outlet, useOutletContext } from 'react-router-dom'
import { api, type GorunumAyari, type OturumKullanici } from '../api/client.ts'
import UstMenu from './UstMenu.tsx'

type DuzenBaglami = {
  kullanici: OturumKullanici
  gorunumGuncelle: (ayar: GorunumAyari) => void
}

// Oturumlu sayfaların ortak çerçevesi: üst menü + sayfa içeriği.
export default function Duzen() {
  const [kullanici, setKullanici] = useState<OturumKullanici | null>(null)
  const [hata, setHata] = useState<string | null>(null)

  useEffect(() => {
    api.ben().then(setKullanici).catch(e => setHata(e.message))
  }, [])

  if (hata) return <div className="tam-ekran-mesaj">{hata}</div>
  if (!kullanici) return <div className="tam-ekran-mesaj">Yükleniyor…</div>

  // Görünüm tercihleri anında uygulanır, arkada users tablosuna kaydedilir (eski siteyle ortak alanlar).
  const gorunumGuncelle = (ayar: GorunumAyari) => {
    const onceki = kullanici
    setKullanici({ ...kullanici, ...ayar })
    api.gorunumKaydet(ayar).catch(() => setKullanici(onceki))
  }

  const baglam: DuzenBaglami = { kullanici, gorunumGuncelle }

  return (
    <div className="uygulama">
      <UstMenu kullanici={kullanici} onGorunum={gorunumGuncelle} />
      <main className="icerik">
        <Outlet context={baglam} />
      </main>
    </div>
  )
}

export function useKullanici() {
  return useOutletContext<DuzenBaglami>().kullanici
}

export function useGorunumGuncelle() {
  return useOutletContext<DuzenBaglami>().gorunumGuncelle
}
