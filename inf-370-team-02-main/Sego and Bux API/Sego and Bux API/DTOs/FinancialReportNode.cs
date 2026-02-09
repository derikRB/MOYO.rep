using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    /// <summary>
    /// A node in the financial‐report hierarchy.
    /// Top‐level “All Time”, then months, then weeks.
    /// </summary>
    public class FinancialReportNode
    {
        public string Period { get; set; } = "";
        public decimal TotalRevenue { get; set; }
        public decimal TotalVAT { get; set; }
        public decimal NetRevenue { get; set; }
        public List<FinancialReportNode>? Children { get; set; }
    }
}
