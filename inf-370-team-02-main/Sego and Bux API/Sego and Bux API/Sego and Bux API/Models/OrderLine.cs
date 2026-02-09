using System;

namespace Sego_and__Bux.Models
{
    public class OrderLine
    {
        public int OrderLineID { get; set; }
        public int OrderID { get; set; }

        // optional now
        public int? ProductID { get; set; }
        public int Quantity { get; set; }

        // snapshots (persisted on order placement)
        public string? ProductNameSnapshot { get; set; }
        public string? ProductImagePathSnapshot { get; set; }

        // navs
        public Order Order { get; set; } = null!;
        public Product? Product { get; set; }
        public Customization? Customization { get; set; }
    }
}
