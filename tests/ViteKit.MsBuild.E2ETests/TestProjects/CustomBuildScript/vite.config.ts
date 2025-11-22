import { defineConfig } from 'vite'

export default defineConfig({
  root: 'wwwroot',
  build: {
    outDir: 'dist',
    emptyOutDir: true
  }
})
