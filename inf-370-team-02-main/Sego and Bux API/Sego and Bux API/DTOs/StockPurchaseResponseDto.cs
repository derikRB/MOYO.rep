using System;
using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    public class StockPurchaseResponseDto
    {
        public int StockPurchaseId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public List<StockPurchaseLineResponseDto> Lines { get; set; } = new();
    }

    public class StockPurchaseLineResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
