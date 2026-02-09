using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sego_and__Bux.Controllers;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using SegoAndBux.Tests.Common; // TestDb.NewContext()
using Xunit;

namespace SegoAndBux.Tests.Controllers
{
    public class ProductController_CreateTests
    {
        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WithResponseDto()
        {
            // Arrange: mock service to return a created ProductResponseDto
            var svc = new Mock<Sego_and__Bux.Interfaces.IProductService>();
            var created = new ProductResponseDto
            {
                ProductID = 123,
                Name = "From Service",
                Price = 10m,
                StockQuantity = 2,
                ProductTypeID = 1,
                LowStockThreshold = 1
            };
            svc.Setup(s => s.AddProductAsync(It.IsAny<ProductDto>()))
               .ReturnsAsync(created);

            using var ctx = TestDb.NewContext();
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.WebRootPath).Returns(System.IO.Path.GetTempPath());

            var controller = new ProductController(svc.Object, ctx, env.Object);

            var dto = new ProductDto
            {
                Name = "Client Name",
                Description = "Client Desc",
                Price = 10m,
                StockQuantity = 2,
                ProductTypeID = 1,
                LowStockThreshold = 1
            };

            // Act
            var result = await controller.Create(dto);

            // Assert
            var createdAt = result as CreatedAtActionResult;
            createdAt.Should().NotBeNull();
            createdAt!.ActionName.Should().Be(nameof(ProductController.Get));

            var payload = createdAt.Value as ProductResponseDto;
            payload.Should().NotBeNull();
            payload!.ProductID.Should().Be(123);
            payload.Name.Should().Be("From Service");

            svc.Verify(s => s.AddProductAsync(It.IsAny<ProductDto>()), Times.Once);
        }
    }
}
