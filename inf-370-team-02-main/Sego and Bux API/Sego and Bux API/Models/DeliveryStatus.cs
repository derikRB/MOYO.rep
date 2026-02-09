namespace Sego_and__Bux.Models
{
    public class DeliveryStatus
    {
        public int DeliveryStatusID { get; set; }
        public string StatusName { get; set; } = "";
        public string Description { get; set; } = "";
        public ICollection<Order>? Orders { get; set; }
    }
}