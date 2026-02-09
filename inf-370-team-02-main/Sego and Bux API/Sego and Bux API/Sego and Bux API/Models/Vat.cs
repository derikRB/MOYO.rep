using System;

namespace Sego_and__Bux.Models
{
    public class Vat
    {
        public int VatId { get; set; }
        public string VatName { get; set; } = null!;
        public decimal Percentage { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
