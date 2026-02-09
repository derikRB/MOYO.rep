using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _svc;
        private readonly ApplicationDbContext _ctx;
        private readonly IWebHostEnvironment _env;

        public ProductController(IProductService svc, ApplicationDbContext ctx, IWebHostEnvironment env)
        {
            _svc = svc; _ctx = ctx; _env = env;
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetAll() =>
            Ok(await _svc.GetAllProductsAsync());

        [HttpGet("{id}"), AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var dto = await _svc.GetProductByIdAsync(id);
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpPost, Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            var created = await _svc.AddProductAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.ProductID }, created);
        }

        [HttpPut("{id}"), Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            var updated = await _svc.UpdateProductAsync(id, dto);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}"), Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteProductAsync(id);
            return Ok(ok);
        }

        [HttpPost("{id}/images"), Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] IFormFile[] files)
        {
            var product = await _ctx.Products.FindAsync(id);
            if (product == null) return NotFound();

            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(folder);

            foreach (var file in files)
            {
                var fn = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var path = Path.Combine(folder, fn);
                await using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                _ctx.ProductImages.Add(new ProductImage
                {
                    ProductID = id,
                    ImagePath = fn,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("import"), Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> ImportBulk([FromBody] List<ProductDto> dtos)
        {
            var results = new List<object>();
            foreach (var dto in dtos)
            {
                try
                {
                    var created = await _svc.AddProductAsync(dto);
                    results.Add(new { name = created.Name, status = "Success", productID = created.ProductID });
                }
                catch (Exception ex)
                {
                    results.Add(new { name = dto.Name, status = "Error", error = ex.Message });
                }
            }
            return Ok(results);
        }

        [HttpDelete("images/{imageId}"), Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var img = await _ctx.ProductImages.FindAsync(imageId);
            if (img == null) return NotFound();

            var full = Path.Combine(_env.WebRootPath, "images", "products", img.ImagePath);
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);

            _ctx.ProductImages.Remove(img);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}
