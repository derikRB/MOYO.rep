using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Controllers
{
    // DTO used by Swagger for multipart/form-data binding
    public sealed class FileUploadDto
    {
        public IFormFile File { get; set; } = default!;
    }

    [ApiController]
    [Route("orders/{orderId:int}/lines/{lineId:int}/customization")]
    public class CustomizationController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IWebHostEnvironment _env;

        public CustomizationController(ApplicationDbContext ctx, IWebHostEnvironment env)
        {
            _ctx = ctx;
            _env = env;
        }

        // POST /orders/{orderId}/lines/{lineId}/customization/upload-image
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public Task<IActionResult> UploadImage(int orderId, int lineId, [FromForm] FileUploadDto model)
            => UploadFile(orderId, lineId, model.File, isSnapshot: false);

        // POST /orders/{orderId}/lines/{lineId}/customization/upload-snapshot
        [HttpPost("upload-snapshot")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public Task<IActionResult> UploadSnapshot(int orderId, int lineId, [FromForm] FileUploadDto model)
            => UploadFile(orderId, lineId, model.File, isSnapshot: true);

        private async Task<IActionResult> UploadFile(int orderId, int lineId, IFormFile file, bool isSnapshot)
        {
            // Fallback (just in case the client used a different field name)
            var actualFile = file ?? Request.Form.Files.FirstOrDefault();
            if (actualFile == null || actualFile.Length == 0)
                return BadRequest(new { message = "No file provided." });

            // Load order line (with customization if it exists)
            var ol = await _ctx.OrderLines
                .Include(x => x.Customization)
                .FirstOrDefaultAsync(x => x.OrderID == orderId && x.OrderLineID == lineId);

            if (ol == null)
                return NotFound(new { message = $"OrderLine {lineId} in Order {orderId} not found." });

            // Ensure folder
            var uploads = Path.Combine(_env.WebRootPath, "customizations");
            Directory.CreateDirectory(uploads);

            // Save file
            var ext = Path.GetExtension(actualFile.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var diskPath = Path.Combine(uploads, fileName);
            await using (var fs = System.IO.File.Create(diskPath))
                await actualFile.CopyToAsync(fs);

            var relative = $"customizations/{fileName}".Replace("\\", "/");
            var publicUrl = $"{Request.Scheme}://{Request.Host}/{relative}";

            // Ensure customization row
            if (ol.Customization == null)
            {
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
                await _ctx.SaveChangesAsync();
            }

            if (isSnapshot)
                ol.Customization.SnapshotPath = relative;
            else
                ol.Customization.UploadedImagePath = relative;

            await _ctx.SaveChangesAsync();

            return Ok(new { imageUrl = publicUrl, path = "/" + relative });
        }
    }
}
