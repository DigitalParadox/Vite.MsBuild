import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    outDir: 'wwwroot/dist',
    manifest: true,
    rollupOptions: {
      input: 'wwwroot/js/main.ts'
    }
  }
})