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
        <Route path="yapim/:id" element={<YapimAsamasinda />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
