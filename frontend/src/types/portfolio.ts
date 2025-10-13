export interface Asset {
  symbol: string
  quantity: number
  averageCost: number
  currentPrice: number
  totalCost: number
  currentValue: number
  return: number
  returnPercentage: number
}

export interface PortfolioSummary {
  totalValue: number
  totalCost: number
  totalReturn: number
  totalReturnPercentage: number
  assetCount: number
  assets: Asset[]
  lastUpdated: string
}

export interface StockPrice {
  symbol: string
  price: number
  timestamp: string
}

export interface ApiError {
  error: string
}

export interface SavedPortfolio {
  id: number
  name: string
  createdDate: string
  purchaseDate: string
  holdings: SavedHolding[]
}

export interface SavedHolding {
  id: number
  portfolioId: number
  symbol: string
  quantity: number
  purchaseDate?: string // Optional - null means use portfolio's default date
}

export interface PortfolioHistoryPoint {
  date: string
  value: number
}

export interface PortfolioHistoryResponse {
  history: PortfolioHistoryPoint[]
}
