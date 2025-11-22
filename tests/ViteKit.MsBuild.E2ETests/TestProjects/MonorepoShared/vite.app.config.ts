import { defineConfig } from 'vite'

export default defineConfig({
  root: 'wwwroot',
  build: {
    outDir: '../wwwroot/app',
    emptyOutDir: true
  }
})
