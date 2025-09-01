# Portfolio Analyzer

A comprehensive portfolio analysis tool built with C# and Python that provides advanced performance metrics, S&P 500 benchmark comparison, and portfolio management capabilities.

## Features

### 📊 Portfolio Analysis
- **Real-time stock data** fetching via Yahoo Finance
- **Performance metrics**: Total return, annualized return, volatility
- **Risk analysis**: Sharpe ratio, maximum drawdown, beta calculation
- **Asset allocation** breakdown with percentages

### 📈 Benchmark Comparison
- **S&P 500 benchmark** analysis using SPY ETF as proxy
- **Alpha calculation** (excess return over market)
- **Beta calculation** (market sensitivity)
- **Visual performance indicators** (🎉 outperform, 📉 underperform)

### 💾 Configuration Management
- **Save/load portfolios** as JSON configurations
- **Persistent storage** in platform-appropriate directories
- **Multiple portfolio** management
- **Command-line flexibility** with configuration override

### 🖥️ Command Line Interface
- **Flexible arguments** for dates, assets, and configurations
- **Interactive help** system
- **Cross-platform compatibility** (Windows, macOS, Linux)

## Installation

### Prerequisites
- **.NET 9.0** or later
- **Python 3.8+** with pip
- **Git** (for cloning)

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PortfolioAnalyzer
   ```

2. **Set up Python environment**
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   pip install yfinance pandas
   ```

3. **Build the .NET application**
   ```bash
   dotnet build
   ```

## Usage

### Basic Usage

```bash
# Run with default portfolio (AAPL + GOOG from 2024-01-01)
dotnet run

# Custom portfolio with specific date
dotnet run -- -d 2023-06-01 -a AAPL:15 -a MSFT:20

# Multiple assets
dotnet run -- -d 2024-01-01 -a NVDA:10 -a AMD:25 -a TSLA:5
```

### Configuration Management

```bash
# Save current portfolio
dotnet run -- -d 2023-01-01 -a AAPL:20 -a GOOG:15 -s

# Load saved portfolio
dotnet run -- -c portfolio

# List all saved configurations
dotnet run -- --list

# Load config and override with new assets
dotnet run -- -c my-portfolio -a MSFT:10
```

### Command Line Options

| Option | Description | Example |
|--------|-------------|---------|
| `-d, --date` | Purchase date (YYYY-MM-DD) | `-d 2023-06-01` |
| `-a, --asset` | Add asset (TICKER:QUANTITY) | `-a AAPL:10` |
| `-c, --config` | Load configuration file | `-c my-portfolio` |
| `-s, --save` | Save current portfolio | `-s` |
| `-l, --list` | List saved configurations | `-l` |
| `-h, --help` | Show help message | `-h` |

## Sample Output

```
=== Tech Portfolio ===
Created: 2025-09-01
Last Updated: 2025-09-01

Portfolio Summary:
Cash: $0.00
Total Value: $9,342.15
Initial Investment: $3,659.38
Total Return: $5,682.77
Assets:
- NVDA: 10 shares at $174.18 each, Total Value: $1,741.80
- MSFT: 15 shares at $506.69 each, Total Value: $7,600.35

=== Performance Metrics ===
Total Return: $5,682.77 (155.29%)
Annualized Return: 42.07%

--- Benchmark Comparison (S&P 500) ---
S&P 500 Total Return: 75.21%
S&P 500 Annualized: 23.38%
Portfolio vs S&P 500: 80.09%
Alpha (excess return): 18.69%
Beta (market sensitivity): 1.18
🎉 Portfolio outperformed the market!

--- Asset Allocation ---
NVDA: 18.6% ($1,741.80)
MSFT: 81.4% ($7,600.35)

--- Risk Metrics ---
Annualized Volatility: 25.09%
Sharpe Ratio: 1.60
Maximum Drawdown: 24.34%
```

## Architecture

### Project Structure
```
PortfolioAnalyzer/
├── Program.cs                 # Main application entry point
├── Models/
│   ├── Asset.cs              # Asset data model
│   └── Portfolio.cs          # Portfolio model with calculations
├── Services/
│   ├── PortfolioService.cs   # Portfolio analysis and metrics
│   └── ConfigurationService.cs # Configuration management
├── Config/
│   └── TickerConfig.cs       # Configuration data models
├── Data/
│   ├── FetchData.py          # Python script for Yahoo Finance API
│   └── ReadData.cs           # C# wrapper for Python data fetching
└── .vscode/
    ├── tasks.json            # Build tasks
    └── launch.json           # Debug configuration
```

### Technology Stack
- **Backend**: C# (.NET 9.0)
- **Data Fetching**: Python with yfinance library
- **Data Storage**: JSON configuration files
- **Financial Data**: Yahoo Finance API via yfinance

## Key Metrics Explained

### Performance Metrics
- **Total Return**: Absolute gain/loss from initial investment
- **Annualized Return**: Yearly return rate (compound annual growth rate)
- **Alpha**: Excess return compared to benchmark (risk-adjusted outperformance)
- **Beta**: Portfolio volatility relative to market (1.0 = same as market)

### Risk Metrics
- **Volatility**: Standard deviation of returns (annualized)
- **Sharpe Ratio**: Risk-adjusted return (higher is better)
- **Maximum Drawdown**: Largest peak-to-trough decline

## Configuration Storage

Configurations are stored in platform-appropriate directories:
- **macOS**: `~/Library/Application Support/PortfolioAnalyzer/`
- **Windows**: `%APPDATA%\PortfolioAnalyzer\`
- **Linux**: `~/.config/PortfolioAnalyzer/`

## Example Portfolios

### Conservative Portfolio
```bash
dotnet run -- -d 2024-01-01 -a SPY:40 -a BND:30 -a VTI:30
```

### Tech Focus
```bash
dotnet run -- -d 2023-01-01 -a AAPL:20 -a MSFT:15 -a NVDA:10 -a GOOG:15
```

### Growth Portfolio
```bash
dotnet run -- -d 2023-06-01 -a TSLA:10 -a NVDA:5 -a AMD:20 -a PLTR:25
```

## Development

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test  # (when tests are added)
```

### Adding New Features
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## Dependencies

### .NET Packages
- System.Text.Json (built-in)
- System.Diagnostics.Process (built-in)

### Python Packages
- yfinance >= 0.2.0
- pandas >= 1.3.0

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- **Yahoo Finance** for providing free financial data via yfinance
- **S&P 500** benchmark comparison using SPY ETF data
- **.NET Foundation** for the excellent development platform

## Roadmap

### Planned Features
- [ ] Multiple benchmark options (NASDAQ, sector ETFs)
- [ ] Dividend tracking and yield calculations
- [ ] Portfolio optimization algorithms
- [ ] Export functionality (PDF, Excel)
- [ ] Real-time price alerts
- [ ] Sector analysis and allocation
- [ ] Monte Carlo simulations
- [ ] Tax-loss harvesting suggestions

### Known Issues
- Requires active internet connection for data fetching
- Historical data limited by Yahoo Finance availability

---

**Built with ❤️ using C# and Python**
