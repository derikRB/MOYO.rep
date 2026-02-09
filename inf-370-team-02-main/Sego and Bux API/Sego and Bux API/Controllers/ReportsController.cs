// Sego_and__Bux/Controllers/ReportsController.cs

using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        public ReportsController(ApplicationDbContext ctx) => _ctx = ctx;

        // --- SalesReport with optional from/to filters ---
        [HttpGet("sales")]
        public ActionResult<List<SalesReport>> GetSales(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null
        )
        {
            var q = _ctx.SalesReports.AsQueryable();

            if (from.HasValue)
                q = q.Where(r =>
                    r.OrderDate.HasValue
                    && r.OrderDate.Value >= from.Value
                );

            if (to.HasValue)
                q = q.Where(r =>
                    r.OrderDate.HasValue
                    && r.OrderDate.Value <= to.Value
                );

            return Ok(q.ToList());
        }

        // --- Other “flat” endpoints ---
        [HttpGet("orders")]
        public ActionResult<List<OrderReport>> GetOrders()
            => Ok(_ctx.OrderReports.ToList());

        [HttpGet("financials")]
        public ActionResult<FinancialReport> GetFinancial()
            => Ok(_ctx.FinancialReports.FirstOrDefault());

        [HttpGet("forecast")]
        public ActionResult<List<StockForecast>> GetForecast()
            => Ok(_ctx.StockForecasts.ToList());

        [HttpGet("stockreport")]
        public ActionResult<List<StockReport>> GetStockReport()
            => Ok(_ctx.StockReports.ToList());

        [HttpGet("ordersbycustomer")]
        public ActionResult<List<OrdersByCustomer>> GetOrdersByCustomer()
            => Ok(_ctx.OrdersByCustomers
                       .OrderBy(x => x.CustomerName)
                       .ThenBy(x => x.OrderDate)
                       .ToList());
        [HttpGet("stocktransactions")]
        public ActionResult<List<StockTransaction>> GetStockTransactions()
      => Ok(_ctx.StockTransactionReports
          .OrderBy(x => x.ProductName)
          .ThenBy(x => x.TranDate ?? DateTime.MinValue)
          .ToList());



        [HttpGet("financials-by-period")]
        public ActionResult<List<FinancialReportByPeriod>> GetFinancialsByPeriod()
        {
            var now = DateTime.UtcNow;
            var vatRateDecimal = (_ctx.Vats
                .OrderByDescending(v => v.EffectiveDate)
                .FirstOrDefault()?.Percentage ?? 0m) / 100m;
            var allOrders = _ctx.Orders.ToList();

            // All Time
            var allTimeTotal = allOrders.Sum(o => (decimal?)o.TotalPrice) ?? 0m;
            var allTimeVat = allTimeTotal * vatRateDecimal;
            var allTimeNet = allTimeTotal - allTimeVat;

            // This Month
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var monthTotal = allOrders
                .Where(o => o.OrderDate >= monthStart && o.OrderDate < nextMonthStart)
                .Sum(o => (decimal?)o.TotalPrice) ?? 0m;
            var monthVat = monthTotal * vatRateDecimal;
            var monthNet = monthTotal - monthVat;

            // Week‐of‐month
            int weekOfMonth = ((now.Day - 1) / 7) + 1;
            var weekLabel = $"Week {weekOfMonth} of {monthStart:MMMM yyyy}";
            var weekStart = monthStart.AddDays((weekOfMonth - 1) * 7);
            var weekEnd = weekStart.AddDays(7) > nextMonthStart
                                ? nextMonthStart
                                : weekStart.AddDays(7);
            var weekTotal = allOrders
                .Where(o => o.OrderDate >= weekStart && o.OrderDate < weekEnd)
                .Sum(o => (decimal?)o.TotalPrice) ?? 0m;
            var weekVat = weekTotal * vatRateDecimal;
            var weekNet = weekTotal - weekVat;

            return Ok(new List<FinancialReportByPeriod> {
                new() {
                    Period       = "All Time",
                    TotalRevenue = allTimeTotal,
                    VatRate      = vatRateDecimal * 100m,
                    TotalVAT     = allTimeVat,
                    NetRevenue   = allTimeNet
                },
                new() {
                    Period       = monthStart.ToString("MMMM yyyy"),
                    TotalRevenue = monthTotal,
                    VatRate      = vatRateDecimal * 100m,
                    TotalVAT     = monthVat,
                    NetRevenue   = monthNet
                },
                new() {
                    Period       = weekLabel,
                    TotalRevenue = weekTotal,
                    VatRate      = vatRateDecimal * 100m,
                    TotalVAT     = weekVat,
                    NetRevenue   = weekNet
                }
            });
        }

        // Hierarchical endpoint
        [HttpGet("financials-hierarchy")]
        public ActionResult<FinancialHierarchyNode> GetFinancialHierarchy()
        {
            var now = DateTime.UtcNow;
            var vatRateDecimal = (_ctx.Vats
                .OrderByDescending(v => v.EffectiveDate)
                .FirstOrDefault()?.Percentage ?? 0m) / 100m;
            var allOrders = _ctx.Orders.ToList();

            // Root node
            var root = new FinancialHierarchyNode
            {
                Period = "All Time",
                TotalRevenue = allOrders.Sum(o => (decimal?)o.TotalPrice) ?? 0m,
                VatRate = vatRateDecimal * 100m
            };
            root.TotalVAT = root.TotalRevenue * vatRateDecimal;
            root.NetRevenue = root.TotalRevenue - root.TotalVAT;

            // Group by month
            var byMonth = allOrders
                .GroupBy(o => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1))
                .OrderBy(g => g.Key);

            foreach (var monthGroup in byMonth)
            {
                var mNode = new FinancialHierarchyNode
                {
                    Period = monthGroup.Key.ToString("MMMM yyyy"),
                    TotalRevenue = monthGroup.Sum(o => (decimal?)o.TotalPrice) ?? 0m,
                    VatRate = vatRateDecimal * 100m
                };
                mNode.TotalVAT = mNode.TotalRevenue * vatRateDecimal;
                mNode.NetRevenue = mNode.TotalRevenue - mNode.TotalVAT;

                var ordersInMonth = monthGroup.ToList();
                int lastDay = DateTime.DaysInMonth(monthGroup.Key.Year, monthGroup.Key.Month);
                int totalWeeks = ((lastDay - 1) / 7) + 1;

                for (int w = 1; w <= totalWeeks; w++)
                {
                    var ws = monthGroup.Key.AddDays((w - 1) * 7);
                    var we = ws.AddDays(7) > monthGroup.Key.AddMonths(1)
                                 ? monthGroup.Key.AddMonths(1)
                                 : ws.AddDays(7);

                    var weekOrders = ordersInMonth
                        .Where(o => o.OrderDate >= ws && o.OrderDate < we)
                        .ToList();
                    if (!weekOrders.Any()) continue;

                    var wNode = new FinancialHierarchyNode
                    {
                        Period = $"Week {w} of {monthGroup.Key:MMMM yyyy}",
                        TotalRevenue = weekOrders.Sum(o => (decimal?)o.TotalPrice) ?? 0m,
                        VatRate = vatRateDecimal * 100m
                    };
                    wNode.TotalVAT = wNode.TotalRevenue * vatRateDecimal;
                    wNode.NetRevenue = wNode.TotalRevenue - wNode.TotalVAT;

                    mNode.Children.Add(wNode);
                }

                root.Children.Add(mNode);
            }

            return Ok(root);
        }
    }
}
