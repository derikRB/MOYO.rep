namespace Sego_and__Bux.Models
{
    public class StockPurchaseLine
    {
        public int StockPurchaseLineId { get; set; }
        public int StockPurchaseId { get; set; }
        public StockPurchase StockPurchase { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
