using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    public class ProductReviewController : ControllerBase
    {
        private readonly IProductReviewService _service;

        public ProductReviewController(IProductReviewService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateProductReviewDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (await _service.HasReviewedAsync(userId, dto.OrderID, dto.ProductID))
                return BadRequest("You have already reviewed this product for this order.");

            var review = await _service.AddReviewAsync(userId, dto);
            return Ok(review);
        }

        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetApprovedForProduct(int productId)
        {
            var reviews = await _service.GetApprovedReviewsByProductAsync(productId);
            return Ok(reviews);
        }

        [HttpGet("has-reviewed")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> HasReviewed([FromQuery] int orderId, [FromQuery] int productId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var exists = await _service.HasReviewedAsync(userId, orderId, productId);
            return Ok(exists);
        }

    }


}