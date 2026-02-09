using System;
using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    // Used to return enriched receipt data (with product names)
    public class StockReceiptResponseDto
    {
        public int StockReceiptId { get; set; }
        public int StockPurchaseId { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
        public List<StockReceiptLineResponseDto> Lines { get; set; } = new();
    }

    public class StockReceiptLineResponseDto
    {
        public int StockReceiptLineId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantityReceived { get; set; }
    }
}
