import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Proxy API calls to the ASP.NET backend during development
      '/settings': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        secure: false
      },
      '/tasks': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        secure: false
      },
      '/notifications': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        secure: false
      },
      '/auth': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        secure: false
      },
      '/health': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    outDir: 'dist',
    sourcemap: true
  }
});
