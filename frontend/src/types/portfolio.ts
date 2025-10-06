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
