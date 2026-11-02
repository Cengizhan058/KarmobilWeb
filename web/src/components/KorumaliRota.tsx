import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { auth } from '../api/client.ts'

export default function KorumaliRota({ children }: { children: ReactNode }) {
  if (!auth.isLoggedIn()) return <Navigate to="/giris" replace />
  return <>{children}</>
}
