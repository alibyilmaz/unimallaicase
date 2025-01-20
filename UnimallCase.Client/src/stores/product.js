import { defineStore } from 'pinia'
import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5011/api'
const API_KEY = 'unimall'

// Create an axios instance with specific header
const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'x-api-key': API_KEY,
    'Content-Type': 'application/json'
  }
})

export const useProductStore = defineStore('product', {
  state: () => ({
    fetchedProducts: [], // List of all fetched products
    loading: false,
    error: null,
    transformedProduct: null
  }),
  
  // Persist all fetched products between sessions
  persist: {
    enabled: true,
    strategies: [
      {
        key: 'product-store',
        storage: localStorage,
        paths: ['fetchedProducts']
      }
    ]
  },
  
  getters: {
    // Return ALL products exactly as they were added
    getAllProducts: (state) => state.fetchedProducts,
    
    // Get product by SKU
    getProductBySku: (state) => {
      return (sku) => state.fetchedProducts.find((p) => p.sku === sku)
    }
  },
  
  actions: {
    async fetchProduct(url) {
      this.loading = true
      this.error = null
      
      try {
        console.log('Fetching product from URL:', url)
        
        const response = await apiClient.get('/Product/crawl-and-transform', {
          params: { url }
        })
        
        // Log the full response for debugging
        console.log('Full API Response:', response.data)
        
        // Store the most recent transformed product
        this.transformedProduct = response.data
        
        // Create a product entry with timestamp and URL
        const productEntry = {
          ...response.data,
          url,  // Include the original URL
          timestamp: Date.now(),  // Add a timestamp
          id: `${response.data.sku}-${Date.now()}` // Unique identifier
        }
        
        // Always add the product to the list EXACTLY as it is
        if (response.data) {
          this.fetchedProducts.push(productEntry)
        }
        
        return productEntry
      } catch (error) {
        console.error('API Error Details:', {
          message: error.message,
          response: error.response?.data,
          status: error.response?.status
        })
        
        this.error = error.response?.data?.detail || error.message
        throw error
      } finally {
        this.loading = false
      }
    },
    
    // Remove a specific product by its unique ID
    removeProduct(id) {
      this.fetchedProducts = this.fetchedProducts.filter(p => p.id !== id)
    },
    
    // Clear all fetched products
    clearProducts() {
      this.fetchedProducts = []
    }
  }
})
