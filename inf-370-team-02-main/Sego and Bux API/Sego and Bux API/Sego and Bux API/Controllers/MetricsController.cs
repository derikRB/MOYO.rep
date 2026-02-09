using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public class MetricsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MetricsController(ApplicationDbContext db) => _db = db;

        // ===== Orders by status (today | last 7 days)
        [HttpGet("orders-by-status")]
        public async Task<ActionResult<IEnumerable<OrderStatusCountDto>>> GetOrdersByStatus([FromQuery] string range = "7d")
        {
            var nowUtc = DateTime.UtcNow;
            var fromUtc = (range?.Equals("today", StringComparison.OrdinalIgnoreCase) ?? false)
                ? nowUtc.Date
                : nowUtc.Date.AddDays(-6);

            // Group by FK only (EF-friendly)
            var grouped = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= fromUtc)
                .GroupBy(o => o.OrderStatusID)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            // Map names in memory
            var names = await _db.OrderStatuses
                .AsNoTracking()
                .ToDictionaryAsync(s => s.OrderStatusID, s => s.OrderStatusName);

            var rows = grouped
                .Select(g => new OrderStatusCountDto(
                    names.TryGetValue(g.Id, out var n) ? n : "Unknown",
                    g.Count))
                .OrderBy(x => x.Status)
                .ToList();

            return Ok(rows);
        }


        // ===== Sales per day (last 30 days)
        [HttpGet("sales-last-30d")]
        public async Task<ActionResult<IEnumerable<SalesPointDto>>> GetSalesLast30d()
        {
            var start = DateTime.UtcNow.Date.AddDays(-29);

            // DB query returns DateTime + decimal (no ToString in SQL)
            var rows = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= start)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalPrice) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fill missing days + format date string on the server
            var map = rows.ToDictionary(x => x.Date, x => x.Total);
            var filled = Enumerable.Range(0, 30)
                .Select(i => start.AddDays(i))
                .Select(d => new SalesPointDto(d.ToString("yyyy-MM-dd"), map.TryGetValue(d, out var t) ? t : 0m))
                .ToList();

            return Ok(filled);
        }

        // ===== Low stock vs threshold (Top N) – this one was already OK
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<LowStockPointDto>>> GetLowStock([FromQuery] int top = 10)
        {
            var data = await _db.Products.AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.StockQuantity - p.LowStockThreshold)
                .Take(top)
                .Select(p => new LowStockPointDto(p.Name, p.StockQuantity, p.LowStockThreshold))
                .ToListAsync();

            return Ok(data);
        }
    }
}