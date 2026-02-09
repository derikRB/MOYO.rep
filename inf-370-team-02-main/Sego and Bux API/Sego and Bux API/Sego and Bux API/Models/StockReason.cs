using System;

namespace Sego_and__Bux.Models
{
    public class StockReason
    {
        public int StockReasonId { get; set; }
        public string Name { get; set; } = string.Empty; // unique display text
        public bool IsActive { get; set; } = true;       // soft-delete
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
    }
}
