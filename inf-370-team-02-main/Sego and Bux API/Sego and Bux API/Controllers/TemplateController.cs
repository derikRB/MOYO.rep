using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateService _svc;
        public TemplateController(ITemplateService svc) => _svc = svc;

        // ─── GET /api/Template ─────────────────────────────
        // returns all templates, with their assigned product IDs
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var list = await _svc.GetAllAsync();
            var dtos = list.Select(t => new TemplateDto
            {
                TemplateID = t.TemplateID,
                Name = t.Name,
                FilePath = t.FilePath,
                ProductIDs = t.ProductTemplates?
                                .Select(pt => pt.ProductID)
                                .ToArray()
            });
            return Ok(dtos);
        }

        // ─── GET /api/Template/ByProduct/{productId} ───────
        [HttpGet("ByProduct/{productId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var list = await _svc.GetByProductAsync(productId);
            var dtos = list.Select(t => new TemplateDto
            {
                TemplateID = t.TemplateID,
                Name = t.Name,
                FilePath = t.FilePath,
                // show only this one ID
                ProductIDs = new[] { productId }
            });
            return Ok(dtos);
        }

        // ─── POST /api/Template ────────────────────────────
        // Admin/Employee only
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create([FromForm] CreateTemplateDto dto)
        {
            var t = await _svc.CreateAsync(dto);
            var result = new TemplateDto
            {
                TemplateID = t.TemplateID,
                Name = t.Name,
                FilePath = t.FilePath,
                ProductIDs = new[] { dto.ProductID }
            };
            return CreatedAtAction(
                nameof(GetByProduct),
                new { productId = dto.ProductID },
                result
            );
        }

        // ─── PUT /api/Template/{id} ────────────────────────
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateTemplateDto dto)
        {
            var t = await _svc.UpdateAsync(id, dto);
            if (t == null) return NotFound();

            var result = new TemplateDto
            {
                TemplateID = t.TemplateID,
                Name = t.Name,
                FilePath = t.FilePath,
                ProductIDs = new[] { dto.ProductID }
            };
            return Ok(result);
        }

        // ─── DELETE /api/Template/{id} ─────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _svc.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}