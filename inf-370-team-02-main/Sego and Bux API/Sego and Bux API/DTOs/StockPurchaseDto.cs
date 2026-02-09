using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    public class StockPurchaseDto
    {
        public string SupplierName { get; set; } = string.Empty;
        public List<StockPurchaseLineDto> Lines { get; set; } = new();
    }

    public class StockPurchaseLineDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
