public class Asset
{
    public string Symbol { get; set; }
    public decimal Quantity { get; set; }
    public List<decimal> HistoricalPrices { get; set; }
    public DateTime PurchaseDate { get; set; }

public Asset(string symbol, DateTime purchaseDate)
{
    Symbol = symbol;
    Quantity = 0;
    HistoricalPrices = new List<decimal>();
    PurchaseDate = purchaseDate;
}

    public decimal CurrentPrice => HistoricalPrices.LastOrDefault();
    public decimal Value => Quantity * CurrentPrice;

}