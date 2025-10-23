import apiClient from './client'
import type {
  PortfolioSummary,
  Asset,
  StockPrice,
  PortfolioHistoryResponse,
  BenchmarkComparison,
} from '@/types/portfolio'

export interface HoldingInput {
  symbol: string
  quantity: number
  purchaseDate?: string // Optional - if not provided, uses portfolio's default date
}

export interface CalculatePortfolioRequest {
  holdings: HoldingInput[]
  purchaseDate: string // ISO date format
}

export const portfolioService = {
  /**
   * Calculate portfolio from user input (no persistence)
   */
  async calculatePortfolio(request: CalculatePortfolioRequest): Promise<PortfolioSummary> {
    const response = await apiClient.post<PortfolioSummary>('/portfolio/calculate', request)
    return response.data
  },

  /**
   * Get demo portfolio summary
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
   * Get portfolio value history over time
   */
  async getPortfolioHistory(request: CalculatePortfolioRequest): Promise<PortfolioHistoryResponse> {
    const response = await apiClient.post<PortfolioHistoryResponse>('/portfolio/history', request)
    return response.data
  },

  /**
   * Get benchmark comparison (S&P 500) for portfolio
   */
  async getBenchmarkComparison(request: CalculatePortfolioRequest): Promise<BenchmarkComparison> {
    const response = await apiClient.post<BenchmarkComparison>('/portfolio/benchmark', request)
    return response.data
  },

  /**
   * Health check endpoint
   */
  async healthCheck(): Promise<{ status: string; timestamp: string }> {
    const response = await apiClient.get('/portfolio/health')
    return response.data
  },
}
