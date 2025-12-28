import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    rollupOptions: {
      external: [
        /^portal-shell\/.*/,
        'react',
        'react-dom',
        'react-router',
        'react-router-dom'
      ]
    }
  }
});
