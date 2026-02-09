using System.Collections.Generic;
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

        // ⇢ Create Purchase
        [HttpPost("purchases")]
        public async Task<ActionResult<StockPurchase>> CapturePurchase([FromBody] StockPurchaseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _svc.CapturePurchaseAsync(dto);
            return CreatedAtAction(nameof(GetPurchase), new { id = created.StockPurchaseId }, created);
        }

        // ⇢ Get Single Purchase
        [HttpGet("purchases/{id}")]
        public async Task<ActionResult<StockPurchase>> GetPurchase(int id)
        {
            var sp = await _svc.GetPurchaseByIdAsync(id);
            return sp is null ? NotFound() : Ok(sp);
        }

        // ⇢ List All Purchases
        [HttpGet("purchases")]
        public async Task<ActionResult<List<StockPurchaseResponseDto>>> GetAllPurchases()
        {
            var list = await _svc.GetAllPurchasesAsync();
            return Ok(list);
        }

        // ⇢ Receive Stock
        [HttpPost("receipts")]
        public async Task<ActionResult<StockReceipt>> ReceiveStock([FromBody] StockReceiptDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        // ⇢ Get Single Receipt (raw entity)
        [HttpGet("receipts/{id}")]
        public async Task<ActionResult<StockReceipt>> GetReceipt(int id)
        {
            var receipt = await _svc.GetReceiptByIdAsync(id);
            return receipt == null ? NotFound() : Ok(receipt);
        }

        // ⇢ List All Receipts (DTO)
        [HttpGet("receipts")]
        public async Task<ActionResult<List<StockReceiptResponseDto>>> GetAllReceipts()
        {
            var list = await _svc.GetAllReceiptsAsync();
            return Ok(list);
        }

        // ⇢ Get Single Receipt (DTO)
        [HttpGet("receipts/{id}/dto")]
        public async Task<ActionResult<StockReceiptResponseDto>> GetReceiptDto(int id)
        {
            var dto = await _svc.GetReceiptByIdDtoAsync(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // ⇢ Adjust Stock
        [HttpPost("adjustments")]
        public async Task<ActionResult<StockAdjustment>> AdjustStock([FromBody] StockAdjustmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        // ⇢ List Adjustments
        [HttpGet("adjustments")]
        public async Task<ActionResult<List<StockAdjustmentResponseDto>>> GetAllAdjustments()
            => Ok(await _svc.GetAllAdjustmentsAsync());

        // ⇢ Get Single Adjustment DTO (optional)
        [HttpGet("adjustments/{id}")]
        public async Task<ActionResult<StockAdjustmentResponseDto>> GetAdjustment(int id)
        {
            var dto = await _svc.GetAdjustmentByIdDtoAsync(id);
            return dto == null ? NotFound() : Ok(dto);
        }

        // ⇢ Low‑Stock Alerts
        [HttpPost("alerts/check")]
        public async Task<ActionResult<List<LowStockAlert>>> CheckLowStock()
        {
            var alerts = await _svc.GenerateLowStockAlertsAsync();
            return Ok(alerts);
        }

        [HttpGet("alerts")]
        public async Task<ActionResult<List<LowStockAlert>>> GetLowStockAlerts()
        {
            var alerts = await _svc.GetAllLowStockAlertsAsync();
            return Ok(alerts);
        }

        [HttpPut("alerts/{id}/resolve")]
        public async Task<IActionResult> ResolveAlert(int id)
        {
            var alert = await _svc.ResolveAlertAsync(id);
            return alert == null ? NotFound() : NoContent();
        }
    }
}
