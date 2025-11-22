import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'path'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot/customer',
    emptyOutDir: true,
    lib: {
      entry: resolve(__dirname, 'wwwroot/js/customer/main.tsx'),
      name: 'CustomerApp',
      formats: ['es'],
      fileName: () => 'app.js'
    },
    rollupOptions: {
      external: ['react', 'react-dom'],
      output: {
        globals: {
          react: 'React',
          'react-dom': 'ReactDOM'
        }
      }
    }
  }
})
