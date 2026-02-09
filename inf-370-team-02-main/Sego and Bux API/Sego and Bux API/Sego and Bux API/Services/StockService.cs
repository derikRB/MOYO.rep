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
        private readonly IAuditWriter _audit;

        public StockService(ApplicationDbContext db, IAuditWriter audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<StockPurchase> CapturePurchaseAsync(StockPurchaseDto dto)
        {
            var saTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "South Africa Standard Time");

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

            await _audit.WriteAsync(
                "Create", "StockPurchase", entity.StockPurchaseId.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { entity.StockPurchaseId, entity.SupplierName, Lines = entity.Lines.Count }),
                criticalValue: $"Lines={entity.Lines.Count}"
            );

            return entity;
        }

        public async Task<StockPurchase?> GetPurchaseByIdAsync(int id) =>
            await _db.StockPurchases
                .Include(sp => sp.Lines).ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(sp => sp.StockPurchaseId == id);

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

        public async Task<StockReceipt> ReceiveStockAsync(StockReceiptDto dto)
        {
            var purchase = await _db.StockPurchases
                .Include(sp => sp.Lines)
                .FirstOrDefaultAsync(sp => sp.StockPurchaseId == dto.StockPurchaseId)
                ?? throw new KeyNotFoundException($"Purchase {dto.StockPurchaseId} not found.");

            var saTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "South Africa Standard Time");

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

            var totalQty = dto.Lines.Sum(x => x.QuantityReceived);
            await _audit.WriteAsync(
                "Receive", "StockReceipt", receipt.StockReceiptId.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { receipt.StockReceiptId, receipt.StockPurchaseId, Lines = dto.Lines.Count }),
                criticalValue: $"TotalReceived={totalQty}"
            );

            return receipt;
        }

        public async Task<StockReceipt?> GetReceiptByIdAsync(int id) =>
            await _db.StockReceipts
                .Include(r => r.Lines).ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(r => r.StockReceiptId == id);

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

        public async Task<StockAdjustment> AdjustStockAsync(StockAdjustmentDto dto)
        {
            var product = await _db.Products.FindAsync(dto.ProductId)
                          ?? throw new KeyNotFoundException($"Product {dto.ProductId} not found.");

            product.StockQuantity += dto.AdjustmentQty;

            if (product.StockQuantity > product.LowStockThreshold)
            {
                var alerts = await _db.LowStockAlerts
                    .Where(a => a.ProductId == product.ProductID && !a.Resolved)
                    .ToListAsync();
                alerts.ForEach(a => a.Resolved = true);
            }

            var saTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "South Africa Standard Time");
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

            await _audit.WriteAsync(
                "Adjust", "StockAdjustment", adj.StockAdjustmentId.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { adj.StockAdjustmentId, adj.ProductId, adj.AdjustmentQty, adj.Reason }),
                criticalValue: $"Adjusted={adj.AdjustmentQty}"
            );

            return adj;
        }

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

        public async Task<List<LowStockAlert>> GenerateLowStockAlertsAsync()
        {
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "South Africa Standard Time");

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

            return alerts;
        }

        public async Task<List<LowStockAlert>> GetAllLowStockAlertsAsync() =>
            await _db.LowStockAlerts
                .Include(a => a.Product)
                .OrderByDescending(a => a.AlertDate)
                .ToListAsync();
        // Services/StockService.cs  (add methods; keep your existing code unchanged)

        public async Task<List<StockReasonResponseDto>> GetAllReasonsAsync(bool includeInactive = false)
        {
            var q = _db.StockReasons.AsQueryable();
            if (!includeInactive) q = q.Where(r => r.IsActive);
            return await q.OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
                .Select(r => new StockReasonResponseDto
                {
                    StockReasonId = r.StockReasonId,
                    Name = r.Name,
                    IsActive = r.IsActive,
                    SortOrder = r.SortOrder,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();
        }

        public async Task<StockReasonResponseDto> CreateReasonAsync(StockReasonDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Reason name is required.");

            if (await _db.StockReasons.AnyAsync(r => r.Name == dto.Name.Trim()))
                throw new InvalidOperationException($"Reason '{dto.Name}' already exists.");

            var entity = new StockReason
            {
                Name = dto.Name.Trim(),
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                CreatedAt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "South Africa Standard Time")
            };

            _db.StockReasons.Add(entity);
            await _db.SaveChangesAsync();

            await _audit.WriteAsync("Create", "StockReason", entity.StockReasonId.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { entity.StockReasonId, entity.Name }),
                criticalValue: entity.Name);

            return new StockReasonResponseDto
            {
                StockReasonId = entity.StockReasonId,
                Name = entity.Name,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<StockReasonResponseDto?> UpdateReasonAsync(int id, StockReasonDto dto)
        {
            var entity = await _db.StockReasons.FindAsync(id);
            if (entity == null) return null;

            // keep historical integrity: adjustments store the TEXT, so renaming here
            // does NOT change past rows.
            var before = System.Text.Json.JsonSerializer.Serialize(new { entity.Name, entity.IsActive, entity.SortOrder });

            if (!string.Equals(entity.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                if (await _db.StockReasons.AnyAsync(r => r.StockReasonId != id && r.Name == dto.Name.Trim()))
                    throw new InvalidOperationException($"Reason '{dto.Name}' already exists.");
                entity.Name = dto.Name.Trim();
            }
            entity.IsActive = dto.IsActive;
            entity.SortOrder = dto.SortOrder;

            await _db.SaveChangesAsync();

            await _audit.WriteAsync("Update", "StockReason", entity.StockReasonId.ToString(),
                beforeJson: before,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { entity.Name, entity.IsActive, entity.SortOrder }),
                criticalValue: entity.Name);

            return new StockReasonResponseDto
            {
                StockReasonId = entity.StockReasonId,
                Name = entity.Name,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<bool> DeleteReasonAsync(int id)
        {
            var entity = await _db.StockReasons.FindAsync(id);
            if (entity == null) return false;

            // soft-delete to preserve dropdown history if reactivated later
            entity.IsActive = false;
            await _db.SaveChangesAsync();

            await _audit.WriteAsync("Delete(Soft)", "StockReason", entity.StockReasonId.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { entity.StockReasonId, entity.IsActive }),
                criticalValue: entity.Name);

            return true;
        }

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
