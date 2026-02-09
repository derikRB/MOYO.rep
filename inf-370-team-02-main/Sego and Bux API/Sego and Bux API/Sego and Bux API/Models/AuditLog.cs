using System;

namespace Sego_and__Bux.Models
{
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime UtcTimestamp { get; set; }

        // “What”
        public string Action { get; set; } = default!;  // e.g., Create/Update/Delete/Adjust
        public string Entity { get; set; } = default!;  // e.g., Order, Product, StockTransaction
        public string? EntityId { get; set; }

        // Snapshots / details
        public string? CriticalValue { get; set; }   // e.g., "Total=123.45, Items=3"
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }

        // “Who”
        public int? CustomerId { get; set; }         // FK → Customers.CustomerID
        public int? EmployeeId { get; set; }         // FK → Employees.EmployeeID
        public string? UserEmailSnapshot { get; set; }
        public string? UserDisplaySnapshot { get; set; } // username or "Name Surname"

        // “Where”
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
    }
}
