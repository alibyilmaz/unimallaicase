<template>
  <div class="card">
    <!-- Product Images -->
    <div class="relative h-64">
      <img 
        :src="product.images[0]" 
        :alt="product.name"
        class="w-full h-full object-cover"
        @error="handleImageError"
      >
      <span class="absolute top-2 right-2 bg-blue-500 text-white px-2 py-1 rounded">
        Score: {{ product.score }}
      </span>
    </div>

    <!-- Product Info -->
    <div class="p-4">
      <router-link 
        :to="{ name: 'product-detail', params: { sku: product.sku }}"
        class="block hover:text-blue-500"
      >
        <h2 class="text-xl font-semibold mb-2 line-clamp-2">{{ product.name }}</h2>
      </router-link>
      
      <p class="text-gray-600 mb-2 line-clamp-2">{{ product.description }}</p>
      
      <div class="flex justify-between items-center mb-2">
        <span class="text-gray-500">{{ product.brand }}</span>
        <span class="text-gray-500">SKU: {{ product.sku }}</span>
      </div>

      <div class="flex justify-between items-center">
        <span class="text-lg font-bold text-green-600">₺{{ product.discountedPrice }}</span>
        <span class="text-gray-400 line-through">₺{{ product.originalPrice }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  product: {
    type: Object,
    required: true
  }
})

function handleImageError(event) {
  event.target.src = 'https://via.placeholder.com/400x400?text=No+Image'
}
</script>
