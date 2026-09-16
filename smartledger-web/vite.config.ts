import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5205',
      '/swagger': 'http://localhost:5205',
      '/hangfire': 'http://localhost:5205',
    },
  },
  build: {
    outDir: '../SmartLedger.API/wwwroot',
    emptyOutDir: true,
  },
})
