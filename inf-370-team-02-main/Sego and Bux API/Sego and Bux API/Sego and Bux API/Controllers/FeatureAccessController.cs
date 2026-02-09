// Sego_and__Bux/Controllers/FeatureAccessController.cs
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/admin/feature-access")]
    public class FeatureAccessController : ControllerBase
    {
        private readonly IFeatureAccessService _svc;
        public FeatureAccessController(IFeatureAccessService svc) => _svc = svc;

        /// <summary>Visible to Admins: full list with effective roles.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<FeatureAccessDto>>> GetAll()
            => Ok(await _svc.GetEffectiveAsync());

        /// <summary>Admins can save multiple rows at once.</summary>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([FromBody] List<FeatureAccessDto> items)
        {
            var by = User?.FindFirst(ClaimTypes.Email)?.Value
                     ?? User?.Identity?.Name
                     ?? "admin";
            await _svc.UpsertBulkAsync(items ?? new(), by);
            return NoContent();
        }

        /// <summary>What features does the current user have (per admin rules)?</summary>
        [HttpGet("my")]
        [Authorize] // any authenticated user
        public async Task<ActionResult<object>> GetMine()
        {
            var set = await _svc.GetAllowedFeaturesForUserAsync(User);
            return Ok(new { features = set.ToArray() });
        }

        /// <summary>Static catalog (key + display) used by clients.</summary>
        [HttpGet("catalog")]
        [Authorize(Roles = "Admin")]
        public ActionResult<object> Catalog()
            => Ok(_svc.Catalog.Select(c => new { key = c.Key, name = c.DisplayName }));
    }
}
