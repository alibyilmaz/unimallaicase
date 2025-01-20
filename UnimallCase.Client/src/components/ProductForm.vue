<template>
  <div>
    <form @submit.prevent="onSubmit" class="flex gap-4">
      <input 
        type="text" 
        v-model="url" 
        placeholder="Enter Trendyol product URL"
        class="input flex-1"
        :disabled="loading"
      >
      <button 
        type="submit" 
        class="btn btn-primary"
        :disabled="loading || !url"
      >
        {{ loading ? 'Loading...' : 'Fetch Product' }}
      </button>
    </form>

    <!-- Display transformed product details -->
    <div v-if="transformedProduct" class="mt-4 p-4 bg-gray-100 rounded">
      <h2 class="text-xl font-bold mb-2">Product Details</h2>
      <div v-if="transformedProduct.name" class="mb-2">
        <strong>Name:</strong> {{ transformedProduct.name }}
      </div>
      <div v-if="transformedProduct.price" class="mb-2">
        <strong>Price:</strong> {{ transformedProduct.price }}
      </div>
      <div v-if="transformedProduct.brand" class="mb-2">
        <strong>Brand:</strong> {{ transformedProduct.brand }}
      </div>
      <div v-if="transformedProduct.description" class="mb-2">
        <strong>Description:</strong> {{ transformedProduct.description }}
      </div>
      <div v-if="transformedProduct.images && transformedProduct.images.length" class="mb-2">
        <strong>Images:</strong>
        <div class="flex gap-2 mt-2">
          <img 
            v-for="(image, index) in transformedProduct.images" 
            :key="index" 
            :src="image" 
            alt="Product Image" 
            class="w-24 h-24 object-cover rounded"
          >
        </div>
      </div>
      
      <!-- Fallback for full object display -->
      <details v-if="Object.keys(transformedProduct).length > 0" class="mt-4">
        <summary class="cursor-pointer text-blue-600">Show Full Details</summary>
        <pre class="bg-white p-2 rounded mt-2 overflow-auto">
{{ JSON.stringify(transformedProduct, null, 2) }}
        </pre>
      </details>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useProductStore } from '../stores/product'

const url = ref('')
const emit = defineEmits(['success', 'error'])
const store = useProductStore()

const loading = computed(() => store.loading)
const transformedProduct = computed(() => store.transformedProduct)

async function onSubmit() {
  try {
    const product = await store.fetchProduct(url.value)
    url.value = ''
    emit('success', product)
  } catch (error) {
    emit('error', error)
  }
}
</script>
