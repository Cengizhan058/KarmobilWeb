import { Navigate, Route, Routes } from 'react-router-dom'
import KorumaliRota from './components/KorumaliRota.tsx'
import Duzen from './components/Duzen.tsx'
import Giris from './pages/Giris.tsx'
import Harita from './pages/Harita.tsx'
import YapimAsamasinda from './pages/YapimAsamasinda.tsx'
import Gecmis from './pages/Gecmis.tsx'
import Rapor from './pages/Rapor.tsx'
import Islemler from './pages/Islemler.tsx'
import AracYonetimi from './pages/AracYonetimi.tsx'
import GrupYonetimi from './pages/GrupYonetimi.tsx'
import ArizaliAraclar from './pages/ArizaliAraclar.tsx'
import CihazSagligi from './pages/CihazSagligi.tsx'
import Giderler from './pages/Giderler.tsx'
import Ruhsat from './pages/Ruhsat.tsx'
import MesaiSaatleri from './pages/MesaiSaatleri.tsx'
import RfidKartlari from './pages/RfidKartlari.tsx'
import Liste from './pages/Liste.tsx'
import Gostergeler from './pages/Gostergeler.tsx'
import Mesajlar from './pages/Mesajlar.tsx'
import Dvr from './pages/Dvr.tsx'
import Destek from './pages/Destek.tsx'
import BildirimAyarlari from './pages/BildirimAyarlari.tsx'

export default function App() {
  return (
    <Routes>
      <Route path="/giris" element={<Giris />} />
      <Route element={<KorumaliRota><Duzen /></KorumaliRota>}>
        <Route index element={<Harita />} />
        <Route path="gecmis" element={<Gecmis />} />
        <Route path="rapor/:kod" element={<Rapor />} />
        <Route path="islemler" element={<Islemler />} />
        <Route path="arac-yonetimi" element={<AracYonetimi />} />
        <Route path="grup-yonetimi" element={<GrupYonetimi />} />
        <Route path="arizali-araclar" element={<ArizaliAraclar />} />
        <Route path="cihaz-sagligi" element={<CihazSagligi />} />
        <Route path="gider/:tur" element={<Giderler />} />
        <Route path="ruhsat" element={<Ruhsat />} />
        <Route path="mesai-saatleri" element={<MesaiSaatleri />} />
        <Route path="rfid" element={<RfidKartlari />} />
        <Route path="liste/:anahtar" element={<Liste />} />
        <Route path="gostergeler" element={<Gostergeler />} />
        <Route path="mesajlar" element={<Mesajlar />} />
        <Route path="dvr" element={<Dvr />} />
        <Route path="bildirim-ayarlari" element={<BildirimAyarlari />} />
        <Route path="destek" element={<Destek />} />
        <Route path="yapim/:id" element={<YapimAsamasinda />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
