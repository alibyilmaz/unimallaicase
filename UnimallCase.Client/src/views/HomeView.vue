<template>
  <div class="container mx-auto px-4 py-8">
    <h1 class="text-3xl font-bold mb-8 text-center">Unimall Product Viewer</h1>
    
    <div class="mb-8">
      <ProductForm />
    </div>

    <div class="mt-8">
      <ProductList />
    </div>
  </div>
</template>

<script setup>
import ProductForm from '../components/ProductForm.vue'
import ProductList from '../components/ProductList.vue'
</script>
