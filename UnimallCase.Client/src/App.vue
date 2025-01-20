<template>
  <div id="app-container">
    <RouterView />
  </div>
</template>

<script setup>
import { RouterView } from 'vue-router'
import { onMounted } from 'vue'

console.log('App.vue script setup running')

onMounted(() => {
  console.log('App.vue mounted')
})
</script>

<style>
/* Ensure the app container takes up full height */
#app-container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
</style>
