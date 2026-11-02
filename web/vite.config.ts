import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // HYS 5273/5274, BelsoftPanel 5173 kullanıyor; çakışmasın diye 5180.
    host: '0.0.0.0',
    port: 5180,
    // Arayüz ve API aynı adresten konuşsun (yayında da API /api altında).
    proxy: {
      '/api': 'http://localhost:5280',
    },
  },
})
