import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import App from './App.vue'
import './assets/tailwind.css'

import 'leaflet/dist/leaflet.css'

console.log("VITE_USER_API =", import.meta.env.DEV.VITE_USER_API);

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.mount('#app')