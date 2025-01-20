<template>
  <div v-if="product" class="container mx-auto px-4 py-8">
    <div class="bg-white rounded-lg shadow-lg overflow-hidden">
      <!-- Product Header -->
      <div class="p-6 bg-gray-50 border-b">
        <div class="flex justify-between items-start">
          <h1 class="text-3xl font-bold">{{ product.name }}</h1>
          <div class="flex items-center gap-4">
            <span class="text-2xl font-bold text-green-600">₺{{ product.discountedPrice }}</span>
            <span class="text-xl text-gray-400 line-through">₺{{ product.originalPrice }}</span>
          </div>
        </div>
        <div class="mt-2 flex items-center justify-between">
          <span class="text-gray-600">{{ product.brand }}</span>
          <div class="flex items-center gap-2">
            <span class="text-gray-500">SKU: {{ product.sku }}</span>
            <span class="bg-blue-500 text-white px-3 py-1 rounded-full">
              Score: {{ product.score }}
            </span>
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-8 p-6">
        <!-- Image Gallery -->
        <div class="space-y-4">
          <div class="aspect-w-4 aspect-h-3 rounded-lg overflow-hidden">
            <img 
              :src="currentImage" 
              :alt="product.name"
              class="w-full h-full object-cover"
              @error="handleImageError"
            >
          </div>
          <div class="grid grid-cols-4 gap-2">
            <button 
              v-for="(image, index) in product.images" 
              :key="index"
              @click="currentImageIndex = index"
              class="aspect-w-1 aspect-h-1 rounded overflow-hidden border-2"
              :class="{ 'border-blue-500': currentImageIndex === index }"
            >
              <img 
                :src="image" 
                :alt="`${product.name} - Image ${index + 1}`"
                class="w-full h-full object-cover"
                @error="handleImageError"
              >
            </button>
          </div>
        </div>

        <!-- Product Details -->
        <div class="space-y-6">
          <div>
            <h2 class="text-xl font-semibold mb-2">Description</h2>
            <p class="text-gray-600">{{ product.description }}</p>
          </div>

          <div>
            <h2 class="text-xl font-semibold mb-2">Category</h2>
            <p class="text-gray-600">{{ product.category }}</p>
          </div>

          <div>
            <h2 class="text-xl font-semibold mb-2">Attributes</h2>
            <div class="grid grid-cols-2 gap-4">
              <div 
                v-for="attr in product.attributes" 
                :key="attr.key"
                class="bg-gray-50 p-3 rounded"
              >
                <span class="font-medium">{{ attr.key }}:</span>
                <span class="text-gray-600 ml-2">{{ attr.name }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="mt-6">
      <router-link 
        to="/" 
        class="btn btn-primary inline-block"
      >
        ← Back to Products
      </router-link>
    </div>
  </div>

  <div v-else class="container mx-auto px-4 py-8 text-center">
    <p class="text-gray-500">Product not found</p>
    <router-link 
      to="/" 
      class="btn btn-primary inline-block mt-4"
    >
      ← Back to Products
    </router-link>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useProductStore } from '../stores/product'

const props = defineProps({
  sku: {
    type: String,
    required: true
  }
})

const store = useProductStore()
const currentImageIndex = ref(0)

const product = computed(() => store.getProductBySku(props.sku))
const currentImage = computed(() => {
  return product.value?.images[currentImageIndex.value] || 'https://via.placeholder.com/400x400?text=No+Image'
})

function handleImageError(event) {
  event.target.src = 'https://via.placeholder.com/400x400?text=No+Image'
}
</script>
