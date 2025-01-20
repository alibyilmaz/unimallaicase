<template>
  <div class="product-list">
    <h2 class="text-2xl font-bold mb-4">Fetched Products</h2>
    
    <div v-if="products.length === 0" class="text-gray-500 text-center p-4 bg-gray-100 rounded">
      No products fetched yet. Start by adding a product URL.
    </div>
    
    <div class="space-y-4">
      <div 
        v-for="product in products" 
        :key="product.id" 
        class="bg-white shadow-md rounded-lg p-4"
      >
        <!-- Product Basic Info -->
        <div class="flex items-start mb-4">
          <!-- Product Image -->
          <div v-if="product.images && product.images.length" class="mr-4 flex-shrink-0">
            <img 
              :src="product.images[0]" 
              :alt="product.name" 
              class="w-24 h-24 object-cover rounded"
            >
          </div>
          
          <!-- Basic Details -->
          <div class="flex-grow">
            <div class="flex justify-between items-start mb-2">
              <div>
                <h3 class="text-lg font-semibold text-gray-800">{{ product.name }}</h3>
                <p class="text-sm text-gray-600">{{ product.brand }}</p>
              </div>
              <span class="text-xs text-gray-500 ml-2">
                {{ formatDate(product.timestamp) }}
              </span>
            </div>

            <!-- Source URL -->
            <div class="mb-2">
              <strong class="text-gray-600 text-sm">Source:</strong>
              <a 
                :href="product.url" 
                target="_blank" 
                class="text-blue-600 hover:underline text-sm ml-2 truncate block"
              >
                {{ product.url }}
              </a>
            </div>
          </div>
        </div>

        <!-- Full Product Details -->
        <details class="bg-gray-50 rounded p-3">
          <summary class="cursor-pointer text-blue-600 font-medium">
            Full Product Details
          </summary>
          
          <!-- Fallback to full JSON if parsed details are empty -->
          <pre class="text-xs bg-white p-2 rounded overflow-auto max-h-64">
{{ getProductDetails(product) }}
          </pre>
        </details>
        
        <!-- Remove Product Button -->
        <div class="mt-3 flex justify-between items-center">
          <button 
            @click="removeProduct(product.id)" 
            class="text-sm text-red-500 hover:text-red-700 transition-colors"
          >
            Remove Product
          </button>
        </div>
      </div>
    </div>
    
    <!-- Clear All Products Button -->
    <div v-if="products.length > 0" class="mt-4 text-center">
      <button 
        @click="clearProducts" 
        class="bg-red-500 text-white px-4 py-2 rounded hover:bg-red-600 transition-colors"
      >
        Clear All Products
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useProductStore } from '../stores/product'

const productStore = useProductStore()

// Get ALL products exactly as they were added
const products = computed(() => productStore.getAllProducts)

// Method to remove a single product
const removeProduct = (id) => {
  productStore.removeProduct(id)
}

// Method to clear all products
const clearProducts = () => {
  productStore.clearProducts()
}

// Format timestamp to readable date
const formatDate = (timestamp) => {
  return new Date(timestamp).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Get product details or full JSON
const getProductDetails = (product) => {
  // Check if the product has a '0' key (nested object)
  if (product[0]) {
    return JSON.stringify(product[0], null, 2)
  }
  
  // If no '0' key, return full product JSON
  return JSON.stringify(product, null, 2)
}
</script>

<style scoped>
details summary::-webkit-details-marker {
  display: none;
}
details summary::marker {
  display: none;
}
details summary {
  list-style: none;
}
</style>
