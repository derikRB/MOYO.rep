using System;
using System.Collections.Generic;

namespace Sego_and__Bux.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public int OrderStatusID { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }

        public string DeliveryMethod { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? CourierProvider { get; set; }

        public string DeliveryStatus { get; set; } = "Pending";
        public string? WaybillNumber { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }   

        public OrderStatus? OrderStatus { get; set; }
        public Customer? Customer { get; set; }

        public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();

        // ← NEW: so EF Core can join back from Customizations
    }
}
//..fvg