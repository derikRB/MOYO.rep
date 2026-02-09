using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/admin/stock")]
    [Authorize(Policy = "InventoryStaff")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _svc;
        public StockController(IStockService svc) => _svc = svc;

        [HttpPost("purchases")]
        public async Task<ActionResult<StockPurchase>> CapturePurchase([FromBody] StockPurchaseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _svc.CapturePurchaseAsync(dto);
            return CreatedAtAction(nameof(GetPurchase), new { id = created.StockPurchaseId }, created);
        }

        [HttpGet("purchases/{id}")]
        public async Task<ActionResult<StockPurchase>> GetPurchase(int id)
        {
            var sp = await _svc.GetPurchaseByIdAsync(id);
            return sp is null ? NotFound() : Ok(sp);
        }

        [HttpGet("purchases")]
        public async Task<ActionResult<List<StockPurchaseResponseDto>>> GetAllPurchases() =>
            Ok(await _svc.GetAllPurchasesAsync());

        [HttpPost("receipts")]
        public async Task<ActionResult<StockReceipt>> ReceiveStock([FromBody] StockReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 🔐 Server decides who performed the action (from JWT)
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            dto.ReceivedBy = staffId;

            try
            {
                var receipt = await _svc.ReceiveStockAsync(dto);
                return CreatedAtAction(nameof(GetReceipt), new { id = receipt.StockReceiptId }, receipt);
            }
            catch (KeyNotFoundException knf)
            {
                return NotFound(knf.Message);
            }
        }

        [HttpGet("receipts/{id}")]
        public async Task<ActionResult<StockReceipt>> GetReceipt(int id)
        {
            var receipt = await _svc.GetReceiptByIdAsync(id);
            return receipt == null ? NotFound() : Ok(receipt);
        }

        [HttpGet("receipts")]
        public async Task<ActionResult<List<StockReceiptResponseDto>>> GetAllReceipts() =>
            Ok(await _svc.GetAllReceiptsAsync());

        [HttpGet("receipts/{id}/dto")]
        public async Task<ActionResult<StockReceiptResponseDto>> GetReceiptDto(int id)
        {
            var dto = await _svc.GetReceiptByIdDtoAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost("adjustments")]
        public async Task<ActionResult<StockAdjustment>> AdjustStock([FromBody] StockAdjustmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 🔐 Server decides who performed the action (from JWT)
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            dto.AdjustedBy = staffId;

            try
            {
                var adj = await _svc.AdjustStockAsync(dto);
                return CreatedAtAction(nameof(GetAdjustment), new { id = adj.StockAdjustmentId }, adj);
            }
            catch (KeyNotFoundException knf)
            {
                return NotFound(knf.Message);
            }
        }

        [HttpGet("adjustments")]
        public async Task<ActionResult<List<StockAdjustmentResponseDto>>> GetAllAdjustments() =>
            Ok(await _svc.GetAllAdjustmentsAsync());

        [HttpGet("adjustments/{id}")]
        public async Task<ActionResult<StockAdjustmentResponseDto>> GetAdjustment(int id)
        {
            var dto = await _svc.GetAdjustmentByIdDtoAsync(id);
            return dto == null ? NotFound() : Ok(dto);
        }

        // ===== Reasons CRUD =====
        [HttpGet("reasons")]
        public async Task<ActionResult<List<StockReasonResponseDto>>> GetReasons([FromQuery] bool includeInactive = false) =>
            Ok(await _svc.GetAllReasonsAsync(includeInactive));

        [HttpPost("reasons")]
        public async Task<ActionResult<StockReasonResponseDto>> CreateReason([FromBody] StockReasonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _svc.CreateReasonAsync(dto);
                return CreatedAtAction(nameof(GetReasons), new { id = created.StockReasonId }, created);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPut("reasons/{id}")]
        public async Task<ActionResult<StockReasonResponseDto>> UpdateReason(int id, [FromBody] StockReasonDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updated = await _svc.UpdateReasonAsync(id, dto);
                return updated is null ? NotFound() : Ok(updated);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpDelete("reasons/{id}")]
        public async Task<IActionResult> DeleteReason(int id) =>
            await _svc.DeleteReasonAsync(id) ? NoContent() : NotFound();

        // ===== Alerts =====
        [HttpPost("alerts/check")]
        public async Task<ActionResult<List<LowStockAlert>>> CheckLowStock() =>
            Ok(await _svc.GenerateLowStockAlertsAsync());

        [HttpGet("alerts")]
        public async Task<ActionResult<List<LowStockAlert>>> GetLowStockAlerts() =>
            Ok(await _svc.GetAllLowStockAlertsAsync());

        [HttpPut("alerts/{id}/resolve")]
        public async Task<IActionResult> ResolveAlert(int id)
        {
            var alert = await _svc.ResolveAlertAsync(id);
            return alert == null ? NotFound() : NoContent();
        }
    }
}
