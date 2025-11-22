import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'wwwroot/dist',
    emptyOutDir: true,
    lib: {
      entry: path.resolve(__dirname, 'wwwroot/js/main.ts'),
      name: 'SingleSpaVue',
      formats: ['es'],
      fileName: () => 'app.js'
    },
    rollupOptions: {
      external: ['vue', 'single-spa-vue'],
      output: {
        globals: {
          vue: 'Vue'
        }
      }
    }
  }
})
