namespace Sego_and__Bux.Models
{
    public class LowStockAlert
    {
        public int LowStockAlertId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int StockQuantity { get; set; }   // current on-hand
        public DateTime AlertDate { get; set; }
        public bool Notified { get; set; }   // has email gone out?
        public bool Resolved { get; set; } = false;
    }
}
