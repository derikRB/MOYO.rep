using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.Interfaces;
using System;
using System.Threading.Tasks;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/admin/productreview")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public class AdminProductReviewController : ControllerBase
    {
        private readonly IProductReviewService _service;

        public AdminProductReviewController(IProductReviewService service) => _service = service;

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
            => Ok(await _service.GetReviewsByStatusAsync("Pending"));

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(string status)
            => Ok(await _service.GetReviewsByStatusAsync(status));

        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _service.GetAllReviewsPagedAsync(page, pageSize));

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            await _service.ApproveReviewAsync(id);
            return NoContent();
        }

        [HttpPut("{id}/decline")]
        public async Task<IActionResult> Decline(int id)
        {
            await _service.DeclineReviewAsync(id);
            return NoContent();
        }

        [HttpPost("bulk-approve")]
        public async Task<IActionResult> BulkApprove([FromBody] int[] ids)
        {
            await _service.BulkApproveAsync(ids ?? Array.Empty<int>());
            return NoContent();
        }

        [HttpPost("bulk-decline")]
        public async Task<IActionResult> BulkDecline([FromBody] int[] ids)
        {
            await _service.BulkDeclineAsync(ids ?? Array.Empty<int>());
            return NoContent();
        }
    }
}
