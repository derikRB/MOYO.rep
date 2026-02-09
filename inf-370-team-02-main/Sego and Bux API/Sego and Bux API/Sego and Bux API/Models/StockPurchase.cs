using System;
using System.Collections.Generic;

namespace Sego_and__Bux.Models
{
    public class StockPurchase
    {
        public int StockPurchaseId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }

        public ICollection<StockPurchaseLine> Lines { get; set; } = new List<StockPurchaseLine>();
        public ICollection<StockReceipt> Receipts { get; set; } = new List<StockReceipt>();
    }
}
