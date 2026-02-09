using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    [Table("StockTransactions")]
    public class StockTransactionEntity
    {
        public int StockTransactionID { get; set; }   // PK
        public int ProductID { get; set; }            // scalar FK only
        public DateTime TranDate { get; set; }
        public int? Received { get; set; }
        public int? Adjusted { get; set; }

        // no navigation here – keeps inserts simple and avoids shadow FKs
        // public Product? Product { get; set; }

    }
}
