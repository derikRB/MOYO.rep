using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services;
using SegoAndBux.Tests.Common; // uses your TestDb.NewContext()
using Xunit;

namespace SegoAndBux.Tests.Services
{
    public class ProductService_AddProductTests
    {
        /// <summary>
        /// Some DBs enforce FK to ProductType. Seed one safely.
        /// </summary>
        private static async Task EnsureProductTypeAsync(ApplicationDbContext ctx)
        {
            if (!await ctx.ProductTypes.AnyAsync())
            {
                ctx.ProductTypes.Add(new ProductType
                {
                    ProductTypeID = 1,
                    ProductTypeName = "Default",
                    Description = "Seeded for tests",
                    CategoryID = 1
                });
                await ctx.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task AddProduct_Persists_MapsFields_WritesAudit()
        {
            using var ctx = TestDb.NewContext();
            await EnsureProductTypeAsync(ctx);

            var audit = new Mock<IAuditWriter>();
            var sut = new ProductService(ctx, audit.Object);

            var dto = new ProductDto
            {
                Name = "Test Product",
                Description = "Unit test product",
                Price = 199.99m,
                StockQuantity = 10,
                ProductTypeID = 1,
                LowStockThreshold = 2
            };

            // Act
            var result = await sut.AddProductAsync(dto);

            // Assert (DTO returned)
            result.Should().NotBeNull();
            result.ProductID.Should().BeGreaterThan(0);
            result.Name.Should().Be("Test Product");
            result.Price.Should().Be(199.99m);
            result.StockQuantity.Should().Be(10);
            result.LowStockThreshold.Should().Be(2);
            result.ProductImages.Should().BeEmpty(); // none yet
            result.PrimaryImage.Should().BeNull();
            result.SecondaryImage.Should().BeNull();

            // Assert (actually persisted)
            var saved = await ctx.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == result.ProductID);
            saved.Should().NotBeNull();
            saved!.Name.Should().Be("Test Product");
            saved.Price.Should().Be(199.99m);

            // Assert (audit called)
            audit.Verify(a => a.WriteAsync(
                "Create", "Product", result.ProductID.ToString(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task AddProduct_WithPrimaryAndSecondaryImageIds_MapsImagesInResponse()
        {
            using var ctx = TestDb.NewContext();
            await EnsureProductTypeAsync(ctx);

            // Seed two images so MapToDto can hydrate Primary/Secondary image DTOs
            var img1 = new ProductImage { ImageID = 10, ImagePath = "p1.jpg" };
            var img2 = new ProductImage { ImageID = 11, ImagePath = "p2.jpg" };
            ctx.ProductImages.AddRange(img1, img2);
            await ctx.SaveChangesAsync();

            var audit = new Mock<IAuditWriter>();
            var sut = new ProductService(ctx, audit.Object);

            var dto = new ProductDto
            {
                Name = "With Images",
                Description = "desc",
                Price = 50m,
                StockQuantity = 1,
                ProductTypeID = 1,
                LowStockThreshold = 1,
                PrimaryImageID = img1.ImageID,
                SecondaryImageID = img2.ImageID
            };

            // Act
            var result = await sut.AddProductAsync(dto);

            // Assert
            result.PrimaryImageID.Should().Be(img1.ImageID);
            result.SecondaryImageID.Should().Be(img2.ImageID);

            // MapToDto builds URLs as "/images/products/{ImagePath}"
            result.PrimaryImage.Should().NotBeNull();
            result.PrimaryImage!.ImageUrl.Should().EndWith("/p1.jpg");

            result.SecondaryImage.Should().NotBeNull();
            result.SecondaryImage!.ImageUrl.Should().EndWith("/p2.jpg");
        }
    }
}
