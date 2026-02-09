using System;
using System.Collections.Generic;

namespace Sego_and__Bux.Models
{
    public class StockReceipt
    {
        public int StockReceiptId { get; set; }
        public int StockPurchaseId { get; set; }
        public StockPurchase StockPurchase { get; set; } = null!;

        public DateTime ReceiptDate { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;  // user/employee name or ID

        public ICollection<StockReceiptLine> Lines { get; set; } = new List<StockReceiptLine>();
    }
}
