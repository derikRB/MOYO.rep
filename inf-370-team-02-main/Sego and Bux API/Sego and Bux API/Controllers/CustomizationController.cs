using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("orders/{orderId}/lines/{lineId}/customization")]
    public class CustomizationController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IWebHostEnvironment _env;

        public CustomizationController(ApplicationDbContext ctx, IWebHostEnvironment env)
        {
            _ctx = ctx;
            _env = env;
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(int orderId, int lineId, IFormFile file)
            => await UploadFile(orderId, lineId, file, "UploadedImagePath");

        [HttpPost("upload-snapshot")]
        public async Task<IActionResult> UploadSnapshot(int orderId, int lineId, IFormFile file)
            => await UploadFile(orderId, lineId, file, "SnapshotPath");

        private async Task<IActionResult> UploadFile(int orderId, int lineId, IFormFile file, string property)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            // Find the OrderLine, include Customization (might be null)
            var ol = await _ctx.OrderLines
                .Include(x => x.Customization)
                .FirstOrDefaultAsync(x => x.OrderID == orderId && x.OrderLineID == lineId);

            if (ol == null)
                return NotFound($"OrderLine {lineId} in Order {orderId} not found.");

            // Create upload folder if missing
            var uploads = Path.Combine(_env.WebRootPath, "customizations");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            // Save the uploaded file
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploads, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            var relative = Path.Combine("customizations", fileName).Replace("\\", "/");
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var publicUrl = $"{baseUrl}/{relative}";

            // ----------- 💡 THE CRITICAL FIX -----------
            if (ol.Customization == null)
            {
                // Create and add a new Customization
                ol.Customization = new Customization
                {
                    OrderLineID = lineId,
                    Template = "",
                    CustomText = "",
                    Font = "",
                    FontSize = 0,
                    Color = ""
                };
                _ctx.Customizations.Add(ol.Customization);
                // Save so we have a valid CustomizationID, if needed
                await _ctx.SaveChangesAsync();
            }

            // Set the property dynamically
            if (property == "UploadedImagePath")
                ol.Customization.UploadedImagePath = relative;
            else if (property == "SnapshotPath")
                ol.Customization.SnapshotPath = relative;

            await _ctx.SaveChangesAsync();
            return Ok(new { imageUrl = publicUrl });
        }
    }
}
