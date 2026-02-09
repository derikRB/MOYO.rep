namespace Sego_and__Bux.Models
{
    public class StockReceiptLine
    {
        public int StockReceiptLineId { get; set; }
        public int StockReceiptId { get; set; }
        public StockReceipt StockReceipt { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int QuantityReceived { get; set; }
    }
}
