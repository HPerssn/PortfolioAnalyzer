import apiClient from './client'
import type { PortfolioSummary, Asset, StockPrice } from '@/types/portfolio'

export const portfolioService = {
  /**
   * Get portfolio summary with all assets and totals
   */
  async getPortfolioSummary(): Promise<PortfolioSummary> {
    const response = await apiClient.get<PortfolioSummary>('/portfolio/summary')
    return response.data
  },

  /**
   * Get list of all assets in portfolio
   */
  async getAssets(): Promise<Asset[]> {
    const response = await apiClient.get<Asset[]>('/portfolio/assets')
    return response.data
  },

  /**
   * Get current price for a specific stock symbol
   */
  async getStockPrice(symbol: string): Promise<StockPrice> {
    const response = await apiClient.get<StockPrice>(`/portfolio/price/${symbol}`)
    return response.data
  },

  /**
   * Health check endpoint
   */
  async healthCheck(): Promise<{ status: string; timestamp: string }> {
    const response = await apiClient.get('/portfolio/health')
    return response.data
  }
}
