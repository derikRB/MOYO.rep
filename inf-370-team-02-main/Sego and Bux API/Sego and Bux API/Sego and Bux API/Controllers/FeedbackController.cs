using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // class-level auth; per-action roles below
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _service;

        public FeedbackController(IFeedbackService service) => _service = service;

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized("Could not determine user identity.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be 1–5.");

            if (await _service.HasGivenFeedbackAsync(userId, dto.OrderID))
                return BadRequest("Feedback for this order already exists.");

            var feedback = await _service.AddFeedbackAsync(userId, dto);
            return Ok(feedback);
        }

        [HttpGet("order/{orderId}")]
        [Authorize(Roles = "Admin,Manager,Employee,Customer")]
        public async Task<IActionResult> GetForOrder(int orderId)
            => Ok(await _service.GetFeedbacksForOrderAsync(orderId));

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMine()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();
            return Ok(await _service.GetFeedbacksByUserAsync(userId));
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllFeedbacksAsync());

        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetForProduct(int productId)
            => Ok(await _service.GetFeedbacksForProductAsync(productId));
    }
}
