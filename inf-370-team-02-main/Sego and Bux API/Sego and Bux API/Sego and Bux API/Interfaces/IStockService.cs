using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface IStockService
    {
        // Purchases
        Task<StockPurchase> CapturePurchaseAsync(StockPurchaseDto dto);
        Task<StockPurchase?> GetPurchaseByIdAsync(int id);
        Task<List<StockPurchaseResponseDto>> GetAllPurchasesAsync();

        // Receipts
        Task<StockReceipt> ReceiveStockAsync(StockReceiptDto dto);
        Task<StockReceipt?> GetReceiptByIdAsync(int id);

        // ⇢ NEW: all receipts as enriched DTOs
        Task<List<StockReceiptResponseDto>> GetAllReceiptsAsync();
        // ⇢ NEW: single receipt as enriched DTO
        Task<StockReceiptResponseDto?> GetReceiptByIdDtoAsync(int id);

        // Adjustments
        Task<StockAdjustment> AdjustStockAsync(StockAdjustmentDto dto);
        // List all past adjustments
        Task<List<StockAdjustmentResponseDto>> GetAllAdjustmentsAsync();
        // o Get one adjustment by ID
        Task<StockAdjustmentResponseDto?> GetAdjustmentByIdDtoAsync(int id);
        // Interfaces/IStockService.cs  (append)
        Task<List<StockReasonResponseDto>> GetAllReasonsAsync(bool includeInactive = false);
        Task<StockReasonResponseDto> CreateReasonAsync(StockReasonDto dto);
        Task<StockReasonResponseDto?> UpdateReasonAsync(int id, StockReasonDto dto);
        Task<bool> DeleteReasonAsync(int id); // soft delete -> IsActive=false

        // Low‑Stock Alerts
        Task<List<LowStockAlert>> GenerateLowStockAlertsAsync();
        Task<List<LowStockAlert>> GetAllLowStockAlertsAsync();
        Task<LowStockAlert?> ResolveAlertAsync(int alertId);
    }
}
