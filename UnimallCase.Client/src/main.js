import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import './assets/main.css'

console.log('Initializing Vue application...')

const app = createApp(App)

app.use(createPinia())
app.use(router)

console.log('Mounting Vue application...')
app.mount('#app')

console.log('Application mounted successfully.')
