namespace Sego_and__Bux.Models
{
    public class OrderStatus
    {
        public int OrderStatusID { get; set; }
        public string OrderStatusName { get; set; } = "";
        public string Description { get; set; } = "";
        public ICollection<Order>? Orders { get; set; }
    }
}
