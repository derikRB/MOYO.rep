// Sego_and__Bux/DTOs/FinancialHierarchyNode.cs
using System;
using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    public class FinancialHierarchyNode
    {
        public string Period { get; set; }            // e.g. "All Time", "July 2025", "Week 2 of July 2025"
        public decimal TotalRevenue { get; set; }
        public decimal VatRate { get; set; }
        public decimal TotalVAT { get; set; }
        public decimal NetRevenue { get; set; }
        public List<FinancialHierarchyNode> Children { get; set; } = new();
    }
}
