import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    lib: {
      entry: 'wwwroot/js/shared/index.ts',
      name: 'Shared',
      fileName: 'shared'
    },
    outDir: 'wwwroot/shared',
    emptyOutDir: true
  }
})
