<template>
  <div class="currency-selector">
    <select v-model="selectedCurrency" @change="handleCurrencyChange" class="currency-select">
      <option
        v-for="(currency, code) in CURRENCIES"
        :key="code"
        :value="code"
      >
        {{ code }} - {{ currency.name }}
      </option>
    </select>
  </div>
</template>

<script setup lang="ts">
import { useCurrencyStore } from '@/stores/currencyStore'
import { CURRENCIES } from '@/constants/currencies'
import { computed } from 'vue'

const currencyStore = useCurrencyStore()

const selectedCurrency = computed({
  get: () => currencyStore.selectedCurrency,
  set: (value) => {
    // Will be called by @change handler
  },
})

const handleCurrencyChange = async (event: Event) => {
  const target = event.target as HTMLSelectElement
  const newCurrency = target.value as keyof typeof CURRENCIES
  currencyStore.setCurrency(newCurrency)

  // Fetch fresh rates when currency is changed
  await currencyStore.fetchExchangeRates()
}
</script>

<style scoped>
.currency-selector {
  display: flex;
  align-items: center;
}

.currency-select {
  padding: 8px 12px;
  border: 1px solid #e5e5e5;
  border-radius: 6px;
  background-color: white;
  color: #262626;
  font-size: 14px;
  cursor: pointer;
  font-family: inherit;
  transition: all 0.2s ease;
}

.currency-select:hover {
  border-color: #f97316;
}

.currency-select:focus {
  outline: none;
  border-color: #f97316;
  box-shadow: 0 0 0 3px rgba(249, 115, 22, 0.1);
}

.currency-select option {
  color: #262626;
  background-color: white;
}
</style>
