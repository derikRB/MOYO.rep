using System;

namespace Sego_and__Bux.Models
{
    public class StockAdjustment
    {
        public int StockAdjustmentId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int AdjustmentQty { get; set; }    // positive or negative
        public string Reason { get; set; } = string.Empty;
        public string AdjustedBy { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; }
    }
}
