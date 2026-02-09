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
    [Authorize(Roles = "Customer")] // Only customers can submit feedback
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _service;

        public FeedbackController(IFeedbackService service)
        {
            _service = service;
        }

        // POST: /api/feedback
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
        {
            // Get user ID from JWT claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized("Could not determine user identity.");

            // Enforce: only 1 feedback per order per user
            if (await _service.HasGivenFeedbackAsync(userId, dto.OrderID))
                return BadRequest("Feedback for this order already exists.");

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be 1–5.");

            var feedback = await _service.AddFeedbackAsync(userId, dto);
            return Ok(feedback);
        }

        // GET: /api/feedback/order/{orderId}
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetForOrder(int orderId)
        {
            var feedbacks = await _service.GetFeedbacksForOrderAsync(orderId);
            return Ok(feedbacks);
        }

        // GET: /api/feedback/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();
            var feedbacks = await _service.GetFeedbacksByUserAsync(userId);
            return Ok(feedbacks);
        }

        // (Admin endpoint) GET: /api/feedback/all
        [HttpGet("all")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> GetAll()
        {
            var feedbacks = await _service.GetAllFeedbacksAsync();
            return Ok(feedbacks);
        }
    }
}