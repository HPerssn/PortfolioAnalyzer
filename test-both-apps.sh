#!/bin/bash

echo "🚀 Portfolio Analyzer - Manual Testing Guide"
echo "============================================="
echo ""

# Change to project root
cd "$(dirname "$0")"

echo "✅ BUILD STATUS:"
if dotnet build --verbosity minimal > /dev/null 2>&1; then
    echo "   ✅ Solution builds successfully!"
    echo "   ✅ All 4 projects compile without errors"
else
    echo "   ❌ Build failed!"
    exit 1
fi

echo ""
echo "🎯 MANUAL TESTING:"
echo ""
echo "1️⃣  TEST CONSOLE APP:"
echo "   Run this command in a terminal:"
echo "   dotnet run --project PortfolioAnalyzer.Console -- -d 2024-01-01 -a AAPL:10 -a GOOGL:5"
echo ""
echo "   Expected result: You should see:"
echo "   • 'Fetching data for AAPL...' and 'Fetching data for GOOGL...'"
echo "   • Portfolio Summary with real stock prices"
echo "   • Performance metrics showing actual returns"
echo ""

echo "2️⃣  TEST API:"
echo "   Step A: Start the API server:"
echo "   dotnet run --project PortfolioAnalyzer.Api"
echo ""
echo "   Step B: In another terminal, test the endpoints:"
echo "   curl http://localhost:5129/api/Portfolio/sample"
echo "   curl http://localhost:5129/api/Portfolio/price/AAPL"
echo ""
echo "   Expected result: JSON responses with portfolio/stock data"
echo ""

echo "🎉 BOTH APPLICATIONS ARE READY TO TEST!"
echo ""
echo "Note: The Console app fetches real-time stock data via Python/yfinance"
echo "Note: The API provides REST endpoints for web integration"
