import { Navigate, Route, Routes } from 'react-router-dom'
import KorumaliRota from './components/KorumaliRota.tsx'
import Duzen from './components/Duzen.tsx'
import Giris from './pages/Giris.tsx'
import Harita from './pages/Harita.tsx'
import YapimAsamasinda from './pages/YapimAsamasinda.tsx'

export default function App() {
  return (
    <Routes>
      <Route path="/giris" element={<Giris />} />
      <Route element={<KorumaliRota><Duzen /></KorumaliRota>}>
        <Route index element={<Harita />} />
        <Route path="yapim/:id" element={<YapimAsamasinda />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
