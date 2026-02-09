using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _db;

        public StockService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ⇢ Capture Purchase
        public async Task<StockPurchase> CapturePurchaseAsync(StockPurchaseDto dto)
        {
            var saTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow, "South Africa Standard Time");

            var entity = new StockPurchase
            {
                SupplierName = dto.SupplierName,
                PurchaseDate = saTime
            };

            foreach (var line in dto.Lines)
            {
                entity.Lines.Add(new StockPurchaseLine
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }

            _db.StockPurchases.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        // ⇢ Get Single Purchase (with products)
        public async Task<StockPurchase?> GetPurchaseByIdAsync(int id) =>
            await _db.StockPurchases
                .Include(sp => sp.Lines).ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(sp => sp.StockPurchaseId == id);

        // ⇢ List All Purchases (DTO)
        public async Task<List<StockPurchaseResponseDto>> GetAllPurchasesAsync() =>
            await _db.StockPurchases
                .Include(p => p.Lines).ThenInclude(l => l.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new StockPurchaseResponseDto
                {
                    StockPurchaseId = p.StockPurchaseId,
                    SupplierName = p.SupplierName,
                    PurchaseDate = p.PurchaseDate,
                    Lines = p.Lines.Select(l => new StockPurchaseLineResponseDto
                    {
                        ProductId = l.ProductId,
                        ProductName = l.Product!.Name,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice
                    }).ToList()
                })
                .ToListAsync();

        // ⇢ Receive Stock (receipt + update)
        public async Task<StockReceipt> ReceiveStockAsync(StockReceiptDto dto)
        {
            var purchase = await _db.StockPurchases
                .Include(sp => sp.Lines)
                .FirstOrDefaultAsync(sp => sp.StockPurchaseId == dto.StockPurchaseId);
            if (purchase == null)
                throw new KeyNotFoundException($"Purchase {dto.StockPurchaseId} not found.");

            var saTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow, "South Africa Standard Time");

            var receipt = new StockReceipt
            {
                StockPurchaseId = dto.StockPurchaseId,
                ReceiptDate = saTime,
                ReceivedBy = dto.ReceivedBy
            };

            foreach (var lineDto in dto.Lines)
            {
                receipt.Lines.Add(new StockReceiptLine
                {
                    ProductId = lineDto.ProductId,
                    QuantityReceived = lineDto.QuantityReceived
                });

                var product = await _db.Products.FindAsync(lineDto.ProductId)
                              ?? throw new KeyNotFoundException($"Product {lineDto.ProductId} not found.");
                product.StockQuantity += lineDto.QuantityReceived;

                if (product.StockQuantity > product.LowStockThreshold)
                {
                    var alerts = await _db.LowStockAlerts
                        .Where(a => a.ProductId == product.ProductID && !a.Resolved)
                        .ToListAsync();
                    alerts.ForEach(a => a.Resolved = true);
                }
            }

            _db.StockReceipts.Add(receipt);
            await _db.SaveChangesAsync();
            return receipt;
        }

        // ⇢ Get Single Receipt (raw)
        public async Task<StockReceipt?> GetReceiptByIdAsync(int id) =>
            await _db.StockReceipts
                .Include(r => r.Lines).ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(r => r.StockReceiptId == id);

        // ⇢ List All Receipts (DTO)
        public async Task<List<StockReceiptResponseDto>> GetAllReceiptsAsync() =>
            await _db.StockReceipts
                .Include(r => r.Lines).ThenInclude(l => l.Product)
                .OrderByDescending(r => r.ReceiptDate)
                .Select(r => new StockReceiptResponseDto
                {
                    StockReceiptId = r.StockReceiptId,
                    StockPurchaseId = r.StockPurchaseId,
                    ReceiptDate = r.ReceiptDate,
                    ReceivedBy = r.ReceivedBy,
                    Lines = r.Lines.Select(l => new StockReceiptLineResponseDto
                    {
                        StockReceiptLineId = l.StockReceiptLineId,
                        ProductId = l.ProductId,
                        ProductName = l.Product!.Name,
                        QuantityReceived = l.QuantityReceived
                    }).ToList()
                })
                .ToListAsync();

        // ⇢ Get Single Receipt (DTO)
        public async Task<StockReceiptResponseDto?> GetReceiptByIdDtoAsync(int id)
        {
            var r = await _db.StockReceipts
                .Include(r => r.Lines).ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(r => r.StockReceiptId == id);
            if (r == null) return null;

            return new StockReceiptResponseDto
            {
                StockReceiptId = r.StockReceiptId,
                StockPurchaseId = r.StockPurchaseId,
                ReceiptDate = r.ReceiptDate,
                ReceivedBy = r.ReceivedBy,
                Lines = r.Lines.Select(l => new StockReceiptLineResponseDto
                {
                    StockReceiptLineId = l.StockReceiptLineId,
                    ProductId = l.ProductId,
                    ProductName = l.Product!.Name,
                    QuantityReceived = l.QuantityReceived
                }).ToList()
            };
        }

        // ⇢ Adjust Stock
        // ⇢ Record a new adjustment
        public async Task<StockAdjustment> AdjustStockAsync(StockAdjustmentDto dto)
        {
            // 1) Find product
            var product = await _db.Products.FindAsync(dto.ProductId)
                          ?? throw new KeyNotFoundException($"Product {dto.ProductId} not found.");

            // 2) Update stock level
            product.StockQuantity += dto.AdjustmentQty;

            // 3) Mark low‑stock alerts resolved if above threshold
            if (product.StockQuantity > product.LowStockThreshold)
            {
                var alerts = await _db.LowStockAlerts
                    .Where(a => a.ProductId == product.ProductID && !a.Resolved)
                    .ToListAsync();
                alerts.ForEach(a => a.Resolved = true);
            }

            // 4) Create adjustment record
            var saTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow,
                           "South Africa Standard Time");
            var adj = new StockAdjustment
            {
                ProductId = dto.ProductId,
                AdjustmentQty = dto.AdjustmentQty,
                Reason = dto.Reason,
                AdjustedBy = dto.AdjustedBy,
                AdjustmentDate = saTime
            };
            _db.StockAdjustments.Add(adj);
            await _db.SaveChangesAsync();
            return adj;
        }

        // ⇢ List all adjustments as DTOs
        public async Task<List<StockAdjustmentResponseDto>> GetAllAdjustmentsAsync() =>
            await _db.StockAdjustments
                .OrderByDescending(a => a.AdjustmentDate)
                .Select(a => new StockAdjustmentResponseDto
                {
                    StockAdjustmentId = a.StockAdjustmentId,
                    ProductId = a.ProductId,
                    AdjustmentQty = a.AdjustmentQty,
                    Reason = a.Reason,
                    AdjustedBy = a.AdjustedBy,
                    AdjustmentDate = a.AdjustmentDate
                })
                .ToListAsync();

        // ⇢ (Optional) Get one adjustment by ID as DTO
        public async Task<StockAdjustmentResponseDto?> GetAdjustmentByIdDtoAsync(int id)
        {
            var a = await _db.StockAdjustments.FindAsync(id);
            if (a == null) return null;
            return new StockAdjustmentResponseDto
            {
                StockAdjustmentId = a.StockAdjustmentId,
                ProductId = a.ProductId,
                AdjustmentQty = a.AdjustmentQty,
                Reason = a.Reason,
                AdjustedBy = a.AdjustedBy,
                AdjustmentDate = a.AdjustmentDate
            };
        }

        // ⇢ Generate Low‑Stock Alerts
        public async Task<List<LowStockAlert>> GenerateLowStockAlertsAsync()
        {
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow, "South Africa Standard Time");

            var lowProducts = await _db.Products
                .Where(p => p.StockQuantity <= p.LowStockThreshold && p.LowStockThreshold > 0)
                .ToListAsync();

            var alerts = new List<LowStockAlert>();
            foreach (var p in lowProducts)
            {
                if (await _db.LowStockAlerts.AnyAsync(a => a.ProductId == p.ProductID && !a.Resolved))
                    continue;

                var alert = new LowStockAlert
                {
                    ProductId = p.ProductID,
                    StockQuantity = p.StockQuantity,
                    AlertDate = now,
                    Notified = false,
                    Resolved = false
                };
                _db.LowStockAlerts.Add(alert);
                alerts.Add(alert);
            }
            await _db.SaveChangesAsync();

            // ← If you implement email notifications, you can send them here.

            return alerts;
        }

        // ⇢ List All Low‑Stock Alerts
        public async Task<List<LowStockAlert>> GetAllLowStockAlertsAsync() =>
            await _db.LowStockAlerts
                .Include(a => a.Product)
                .OrderByDescending(a => a.AlertDate)
                .ToListAsync();

        // ⇢ Resolve an Alert
        public async Task<LowStockAlert?> ResolveAlertAsync(int alertId)
        {
            var alert = await _db.LowStockAlerts.FindAsync(alertId);
            if (alert == null) return null;
            alert.Resolved = true;
            await _db.SaveChangesAsync();
            return alert;
        }
    }
}
//djtgi