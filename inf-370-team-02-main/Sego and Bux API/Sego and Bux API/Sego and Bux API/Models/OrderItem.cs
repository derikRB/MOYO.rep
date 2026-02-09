namespace Sego_and__Bux.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }   // ✅ instead of "Price"

        // Navigation
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
