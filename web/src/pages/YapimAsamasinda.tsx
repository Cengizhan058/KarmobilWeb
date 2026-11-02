import { useSearchParams } from 'react-router-dom'
import { Hammer } from 'lucide-react'

export default function YapimAsamasinda() {
  const [params] = useSearchParams()
  return (
    <div className="yapim">
      <Hammer size={40} />
      <h2>{params.get('ad') ?? 'Bu ekran'}</h2>
      <p>Bu ekran yeni arayüze henüz taşınmadı.</p>
    </div>
  )
}
