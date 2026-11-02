import { useEffect, useState } from 'react'
import { Outlet, useOutletContext } from 'react-router-dom'
import { api, type OturumKullanici } from '../api/client.ts'
import UstMenu from './UstMenu.tsx'

// Oturumlu sayfaların ortak çerçevesi: üst menü + sayfa içeriği.
export default function Duzen() {
  const [kullanici, setKullanici] = useState<OturumKullanici | null>(null)
  const [hata, setHata] = useState<string | null>(null)

  useEffect(() => {
    api.ben().then(setKullanici).catch(e => setHata(e.message))
  }, [])

  if (hata) return <div className="tam-ekran-mesaj">{hata}</div>
  if (!kullanici) return <div className="tam-ekran-mesaj">Yükleniyor…</div>

  return (
    <div className="uygulama">
      <UstMenu kullanici={kullanici} />
      <main className="icerik">
        <Outlet context={kullanici} />
      </main>
    </div>
  )
}

export function useKullanici() {
  return useOutletContext<OturumKullanici>()
}
