using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public int OrderStatusID { get; set; }

        // Delivery details
        public string DeliveryMethod { get; set; } = "HandToHand";
        public string? DeliveryAddress { get; set; }
        public string? CourierProvider { get; set; }
        public string? DeliveryStatus { get; set; }
        public string? WaybillNumber { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }

        // ✅ Navigation
        public Customer? Customer { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }


        public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
