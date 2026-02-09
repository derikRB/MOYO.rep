// vite.config.ts
import { defineConfig } from 'vite';
import createAngularPlugin from '@analogjs/vite-plugin-angular';

export default defineConfig({
  plugins: [
    // Angular plugin (default export), no need to change
    createAngularPlugin()
  ],

  // Force Vite to prebundle this dep so that dynamic-imports work
  optimizeDeps: {
    include: [
      // point directly at the UMD bundle
      'html2pdf.js/dist/html2pdf.bundle'
    ]
  },

  // Help TS/Vite resolve `import("html2pdf.js/dist/html2pdf.bundle")`
  resolve: {
    alias: {
      // whenever you `import 'html2pdf.js'` under the hood,
      // actually load the bundled file
      'html2pdf.js': 'html2pdf.js/dist/html2pdf.bundle.js'
    }
  }
});
