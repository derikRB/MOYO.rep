// Models/StockTransactionEntity.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    [Table("StockTransactions")]
    public class StockTransactionEntity
    {
        public int StockTransactionID { get; set; } // PK!
        public int ProductID { get; set; }
        public DateTime TranDate { get; set; }
        public int? Received { get; set; }
        public int? Adjusted { get; set; }

        // Navigation (optional)
        public Product Product { get; set; }
    }

}
