import yfinance as yf
import pandas as pd
import sys
import json

# Read arguments: ticker and start date
ticker = sys.argv[1]
start_date = sys.argv[2]

# Download historical data
data = yf.download(ticker, start=start_date, interval="1d", auto_adjust=True)

# Reset index to make Date a column
data = data.reset_index()

# Handle multi-level columns
if isinstance(data.columns, pd.MultiIndex):
    # Find the Close column (it will have the ticker in the second level)
    close_col = None
    for col in data.columns:
        if col[0] == 'Close':
            close_col = col
            break
    
    # Create new DataFrame with simplified column names
    result_data = pd.DataFrame({
        'Date': data['Date'],
        'Close': data[close_col] if close_col else data.iloc[:, -1]  # Fallback to last column
    })
else:
    # Simple columns (single ticker case)
    result_data = data[["Date", "Close"]]

# Convert Date to string for JSON
result_data['Date'] = result_data['Date'].dt.strftime('%Y-%m-%d')

# Output JSON
print(result_data.to_json(orient="records"))