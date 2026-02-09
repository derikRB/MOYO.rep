using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sego_and__Bux.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _ctx;
        public ProductService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<ProductResponseDto> AddProductAsync(ProductDto dto)
        {
            var p = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ProductTypeID = dto.ProductTypeID,
                PrimaryImageID = dto.PrimaryImageID,
                SecondaryImageID = dto.SecondaryImageID,
                LowStockThreshold = dto.LowStockThreshold,   // ← ADD THIS LINE


            };
            _ctx.Products.Add(p);
            await _ctx.SaveChangesAsync();
            return await MapToDtoAsync(p.ProductID);
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync()
        {
            var list = await _ctx.Products
                .Include(p => p.PrimaryImage)
                .Include(p => p.ProductImages)
                .Include(p => p.SecondaryImage)
                .Include(p => p.ProductType)

                .ToListAsync();

            return list.Select(MapToDto).ToList();
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
        {
            var p = await _ctx.Products
                .Include(x => x.PrimaryImage)
                .Include(x => x.ProductImages)
                .Include(x => x.ProductType)
                .FirstOrDefaultAsync(x => x.ProductID == id);
            return p == null ? null : MapToDto(p);
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(int id, ProductDto dto)
        {
            var p = await _ctx.Products.FindAsync(id);
            if (p == null) return null;

            p.Name = dto.Name;
            p.Description = dto.Description;
            p.Price = dto.Price;
            p.StockQuantity = dto.StockQuantity;
            p.ProductTypeID = dto.ProductTypeID;
            p.PrimaryImageID = dto.PrimaryImageID;
            p.SecondaryImageID = dto.SecondaryImageID;
            p.LowStockThreshold = dto.LowStockThreshold;  // <<< ADD THIS LINE



            await _ctx.SaveChangesAsync();
            return await MapToDtoAsync(id);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var p = await _ctx.Products.FindAsync(id);
            if (p == null) return false;
            _ctx.Products.Remove(p);
            await _ctx.SaveChangesAsync();
            return true;
        }

        // ─── helper mappers ─────────────────────────────────

        private ProductResponseDto MapToDto(Product p)
        {
            var dto = new ProductResponseDto
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ProductTypeID = p.ProductTypeID,
                ProductType = p.ProductType == null ? null : new ProductTypeDto
                {
                    ProductTypeID = p.ProductType.ProductTypeID,
                    ProductTypeName = p.ProductType.ProductTypeName,
                    Description = p.ProductType.Description,
                    CategoryID = p.ProductType.CategoryID
                },
                PrimaryImageID = p.PrimaryImageID,
                SecondaryImageID = p.SecondaryImageID,
                LowStockThreshold = p.LowStockThreshold,
                ProductImages = p.ProductImages
                    .Select(i => new ProductImageDto
                    {
                        ImageID = i.ImageID,
                        ImageUrl = "/images/products/" + i.ImagePath
                    })
                    .ToList()
            };

            if (p.PrimaryImage != null)
            {
                dto.PrimaryImage = new ProductImageDto
                {
                    ImageID = p.PrimaryImage.ImageID,
                    ImageUrl = "/images/products/" + p.PrimaryImage.ImagePath
                };
            }
            if (p.SecondaryImage != null)
            {
                dto.SecondaryImage = new ProductImageDto
                {
                    ImageID = p.SecondaryImage.ImageID,
                    ImageUrl = "/images/products/" + p.SecondaryImage.ImagePath
                };
            }

            return dto;
        }


        private async Task<ProductResponseDto> MapToDtoAsync(int id)
        {
            var p = await _ctx.Products
                .Include(x => x.PrimaryImage)
                .Include(p => p.SecondaryImage)
                .Include(x => x.ProductImages)
                .FirstAsync(x => x.ProductID == id);
            return MapToDto(p);
        }
    }
}
