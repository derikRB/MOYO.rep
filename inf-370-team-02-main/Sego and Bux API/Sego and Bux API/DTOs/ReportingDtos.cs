using System;
using Microsoft.EntityFrameworkCore;

namespace Sego_and__Bux.DTOs
{
    [Keyless]
    public class SalesReport
    {
        public DateTime? OrderDate { get; set; } // Make nullable if possible null
        public int? Orders { get; set; }
        public decimal? Revenue { get; set; }
    }

    [Keyless]
    public class OrderReport
    {
        public int? OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string OrderStatusName { get; set; } = "";
        public decimal? TotalPrice { get; set; }
        public string DeliveryMethod { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
    }

    [Keyless]
    public class FinancialReport
    {
        public decimal? TotalRevenue { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? TotalVAT { get; set; }
        public decimal? NetRevenue { get; set; }
    }

    [Keyless]
    public class StockForecast
    {
        public int? ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public double? AvgDailyQty { get; set; }
        public double? Predicted7DayQty { get; set; }
    }

    [Keyless]
    public class StockReport
    {
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public int? TotalStock { get; set; }
    }

    [Keyless]
    public class OrdersByCustomer
    {
        public int? CustomerID { get; set; }
        public string CustomerName { get; set; } = "";
        public int? OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Status { get; set; } = "";
        public decimal? Total { get; set; }
    }

    [Keyless]
    public class StockTransaction
    {
        public int? ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public DateTime? TranDate { get; set; }
        public int? Received { get; set; }
        public int? Adjusted { get; set; }
    }
    [Keyless]
    public class FinancialReportByPeriod
    {
        public string Period { get; set; } // e.g. "All Time", "Monthly", "Weekly"
        public decimal? TotalRevenue { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? TotalVAT { get; set; }
        public decimal? NetRevenue { get; set; }
    }


}
