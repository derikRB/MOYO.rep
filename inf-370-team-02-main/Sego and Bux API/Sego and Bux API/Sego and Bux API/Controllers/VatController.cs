using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.Dto;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services.Interfaces;
using System.Threading.Tasks;
using System;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/vat")]
    public class VatController : ControllerBase
    {
        private readonly IVatService _vatService;
        public VatController(IVatService vatService) => _vatService = vatService;

        // 1. Public: fetch the single active VAT rate
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<Vat>> GetActive()
        {
            var active = await _vatService.GetActiveAsync();
            if (active == null)
                return NotFound("No active VAT rate configured.");
            return Ok(active);
        }

        // 2. Staff-only: list all VAT rates
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<ActionResult<List<Vat>>> GetAll()
        {
            var list = await _vatService.GetAllAsync();
            return Ok(list);
        }

        // 3. Staff-only: create a new VAT rate (NO DUPLICATES ALLOWED)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<ActionResult<Vat>> Create([FromBody] VatDto dto)
        {
            if (dto.Percentage < 0 || dto.Percentage > 100)
                return BadRequest("Percentage must be between 0 and 100.");

            // --- Duplicate check before proceeding ---
            var exists = await _vatService.ExistsByDateAsync(dto.EffectiveDate);
            if (exists)
                return BadRequest("A VAT rate for this effective date already exists.");

            var created = await _vatService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetActive), new { }, created);
        }

        // 4. Staff-only: update an existing VAT rate (without activating)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<ActionResult<Vat>> Update(int id, [FromBody] VatDto dto)
        {
            // --- Duplicate check (ignore self) ---
            var exists = await _vatService.ExistsByDateForOtherIdAsync(dto.EffectiveDate, id);
            if (exists)
                return BadRequest("A VAT rate for this effective date already exists.");

            var updated = await _vatService.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // 5. Staff-only: activate a given past VAT rate
        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<ActionResult<Vat>> Activate(int id)
        {
            var activated = await _vatService.ActivateAsync(id);
            if (activated == null) return NotFound();
            return Ok(activated);
        }
    }
}
//fsfdgf