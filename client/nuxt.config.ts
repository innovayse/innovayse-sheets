// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss'],
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    public: {
      sheetsApiUrl: process.env.NUXT_PUBLIC_SHEETS_API_URL || 'http://localhost:5080',
      mainUrl: process.env.NUXT_PUBLIC_MAIN_URL || 'http://app.local'
    }
  },
  vite: {
    server: {
      // Dev-only: the stack is reached over NPM at *.local hosts, so the Vite dev server
      // must accept the sheets.local Host header (Vite blocks unknown hosts by default).
      allowedHosts: ['sheets.local']
    }
  }
})
