using System;
using System.Linq;

public class BenchmarkMetrics
{
    public decimal TotalReturn { get; set; }
    public decimal AnnualizedReturn { get; set; }
    public List<decimal> DailyReturns { get; set; } = new List<decimal>();
}

public class PortfolioService
{
    private readonly Portfolio _portfolio;

    public PortfolioService(Portfolio portfolio)
    {
        _portfolio = portfolio;
    }

    public void PrintPortfolioSummary()
    {
        _portfolio.PrintSummary();
    }

    public decimal CalculateTotalReturn()
    {
        return _portfolio.CalculateTotalReturn();
    }

    public void PrintPerformanceMetrics()
    {
        Console.WriteLine("\n=== Performance Metrics ===");
        
        // Basic metrics
        var totalReturn = CalculateTotalReturn();
        var totalReturnPercent = CalculateTotalReturnPercentage();
        var annualizedReturn = CalculateAnnualizedReturn();
        
        Console.WriteLine($"Total Return: {totalReturn:C} ({totalReturnPercent:P2})");
        Console.WriteLine($"Annualized Return: {annualizedReturn:P2}");
        
        // Benchmark comparison
        PrintBenchmarkComparison();
        
        // Asset allocation
        PrintAssetAllocation();
        
        // Risk metrics
        PrintRiskMetrics();
    }

    public decimal CalculateTotalReturnPercentage()
    {
        var initialInvestment = _portfolio.CalculateInitialInvestment();
        if (initialInvestment == 0) return 0;
        
        return _portfolio.CalculateTotalReturn() / initialInvestment;
    }

    public decimal CalculateAnnualizedReturn()
    {
        if (!_portfolio.Assets.Any()) return 0;
        
        // Use the purchase date from the first asset
        var purchaseDate = _portfolio.Assets.First().PurchaseDate;
        var daysSincePurchase = (DateTime.Now - purchaseDate).TotalDays;
        
        if (daysSincePurchase <= 0) return 0;
        
        var totalReturnPercent = CalculateTotalReturnPercentage();
        var years = daysSincePurchase / 365.25;
        
        // Annualized return formula: (1 + total_return)^(1/years) - 1
        return (decimal)(Math.Pow((double)(1 + totalReturnPercent), 1 / years) - 1);
    }

    public decimal CalculatePortfolioVolatility()
    {
        if (!_portfolio.Assets.Any()) return 0;
        
        var returns = CalculateDailyReturns();
        if (returns.Count < 2) return 0;
        
        var avgReturn = returns.Average();
        var variance = returns.Sum(r => (decimal)Math.Pow((double)(r - avgReturn), 2)) / (returns.Count - 1);
        var dailyVolatility = (decimal)Math.Sqrt((double)variance);
        
        // Annualize volatility (multiply by sqrt of 252 trading days)
        return dailyVolatility * (decimal)Math.Sqrt(252);
    }

    public decimal CalculateSharpeRatio()
    {
        var annualizedReturn = CalculateAnnualizedReturn();
        var volatility = CalculatePortfolioVolatility();
        var riskFreeRate = 0.02m; // Assume 2% risk-free rate
        
        if (volatility == 0) return 0;
        
        return (annualizedReturn - riskFreeRate) / volatility;
    }

    public decimal CalculateMaxDrawdown()
    {
        var dailyValues = CalculateDailyPortfolioValues();
        if (dailyValues.Count < 2) return 0;
        
        decimal maxDrawdown = 0;
        decimal peak = dailyValues[0];
        
        foreach (var value in dailyValues)
        {
            if (value > peak)
                peak = value;
            
            var drawdown = (peak - value) / peak;
            if (drawdown > maxDrawdown)
                maxDrawdown = drawdown;
        }
        
        return maxDrawdown;
    }

    private void PrintAssetAllocation()
    {
        Console.WriteLine("\n--- Asset Allocation ---");
        var totalValue = _portfolio.TotalValue;
        
        foreach (var asset in _portfolio.Assets)
        {
            var allocation = totalValue > 0 ? asset.Value / totalValue : 0;
            Console.WriteLine($"{asset.Symbol}: {allocation:P1} ({asset.Value:C})");
        }
    }

    private void PrintRiskMetrics()
    {
        Console.WriteLine("\n--- Risk Metrics ---");
        
        var volatility = CalculatePortfolioVolatility();
        var sharpeRatio = CalculateSharpeRatio();
        var maxDrawdown = CalculateMaxDrawdown();
        
        Console.WriteLine($"Annualized Volatility: {volatility:P2}");
        Console.WriteLine($"Sharpe Ratio: {sharpeRatio:F2}");
        Console.WriteLine($"Maximum Drawdown: {maxDrawdown:P2}");
    }

    private void PrintBenchmarkComparison()
    {
        Console.WriteLine("\n--- Benchmark Comparison (S&P 500) ---");
        
        try
        {
            var benchmarkMetrics = CalculateBenchmarkMetrics();
            
            if (benchmarkMetrics != null)
            {
                var portfolioReturn = CalculateTotalReturnPercentage();
                var portfolioAnnualized = CalculateAnnualizedReturn();
                var alpha = portfolioAnnualized - benchmarkMetrics.AnnualizedReturn;
                var beta = CalculateBeta(benchmarkMetrics.DailyReturns);
                
                Console.WriteLine($"S&P 500 Total Return: {benchmarkMetrics.TotalReturn:P2}");
                Console.WriteLine($"S&P 500 Annualized: {benchmarkMetrics.AnnualizedReturn:P2}");
                Console.WriteLine($"Portfolio vs S&P 500: {portfolioReturn - benchmarkMetrics.TotalReturn:P2}");
                Console.WriteLine($"Alpha (excess return): {alpha:P2}");
                Console.WriteLine($"Beta (market sensitivity): {beta:F2}");
                
                if (alpha > 0)
                    Console.WriteLine("🎉 Portfolio outperformed the market!");
                else
                    Console.WriteLine("📉 Portfolio underperformed the market");
            }
            else
            {
                Console.WriteLine("Unable to fetch S&P 500 benchmark data");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating benchmark: {ex.Message}");
        }
    }

    private BenchmarkMetrics? CalculateBenchmarkMetrics()
    {
        if (!_portfolio.Assets.Any()) return null;
        
        try
        {
            // Use the earliest purchase date from portfolio
            var earliestDate = _portfolio.Assets.Min(a => a.PurchaseDate);
            
            // Fetch S&P 500 data (using SPY ETF as proxy)
            var spyPrices = ReadData.FetchPricesFromPython("SPY", earliestDate);
            
            if (!spyPrices.Any()) return null;
            
            var spyPriceList = spyPrices.Select(p => p.Close).ToList();
            var initialPrice = spyPriceList.First();
            var currentPrice = spyPriceList.Last();
            
            var totalReturn = (currentPrice - initialPrice) / initialPrice;
            var daysSincePurchase = (DateTime.Now - earliestDate).TotalDays;
            var years = daysSincePurchase / 365.25;
            var annualizedReturn = (decimal)(Math.Pow((double)(1 + totalReturn), 1 / years) - 1);
            
            // Calculate daily returns for beta calculation
            var dailyReturns = new List<decimal>();
            for (int i = 1; i < spyPriceList.Count; i++)
            {
                if (spyPriceList[i - 1] > 0)
                {
                    var dailyReturn = (spyPriceList[i] - spyPriceList[i - 1]) / spyPriceList[i - 1];
                    dailyReturns.Add(dailyReturn);
                }
            }
            
            return new BenchmarkMetrics
            {
                TotalReturn = totalReturn,
                AnnualizedReturn = annualizedReturn,
                DailyReturns = dailyReturns
            };
        }
        catch
        {
            return null;
        }
    }

    private decimal CalculateBeta(List<decimal> benchmarkReturns)
    {
        try
        {
            var portfolioReturns = CalculateDailyReturns();
            
            // Ensure we have matching data points
            var minLength = Math.Min(portfolioReturns.Count, benchmarkReturns.Count);
            if (minLength < 2) return 1.0m; // Default beta of 1
            
            portfolioReturns = portfolioReturns.Take(minLength).ToList();
            benchmarkReturns = benchmarkReturns.Take(minLength).ToList();
            
            var portfolioMean = portfolioReturns.Average();
            var benchmarkMean = benchmarkReturns.Average();
            
            decimal covariance = 0;
            decimal benchmarkVariance = 0;
            
            for (int i = 0; i < minLength; i++)
            {
                var portfolioDeviation = portfolioReturns[i] - portfolioMean;
                var benchmarkDeviation = benchmarkReturns[i] - benchmarkMean;
                
                covariance += portfolioDeviation * benchmarkDeviation;
                benchmarkVariance += benchmarkDeviation * benchmarkDeviation;
            }
            
            if (benchmarkVariance == 0) return 1.0m;
            
            return covariance / benchmarkVariance;
        }
        catch
        {
            return 1.0m; // Default beta
        }
    }

    private List<decimal> CalculateDailyReturns()
    {
        var dailyValues = CalculateDailyPortfolioValues();
        var returns = new List<decimal>();
        
        for (int i = 1; i < dailyValues.Count; i++)
        {
            if (dailyValues[i - 1] > 0)
            {
                var dailyReturn = (dailyValues[i] - dailyValues[i - 1]) / dailyValues[i - 1];
                returns.Add(dailyReturn);
            }
        }
        
        return returns;
    }

    private List<decimal> CalculateDailyPortfolioValues()
    {
        if (!_portfolio.Assets.Any()) return new List<decimal>();
        
        // Get the minimum number of price points across all assets
        var minPriceCount = _portfolio.Assets.Min(a => a.HistoricalPrices.Count);
        var dailyValues = new List<decimal>();
        
        for (int i = 0; i < minPriceCount; i++)
        {
            decimal dailyValue = 0;
            foreach (var asset in _portfolio.Assets)
            {
                if (i < asset.HistoricalPrices.Count)
                {
                    dailyValue += asset.HistoricalPrices[i] * asset.Quantity;
                }
            }
            dailyValues.Add(dailyValue);
        }
        
        return dailyValues;
    }
}