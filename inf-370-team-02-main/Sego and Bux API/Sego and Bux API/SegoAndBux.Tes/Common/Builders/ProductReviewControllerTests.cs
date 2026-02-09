using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sego_and__Bux.Controllers;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Xunit;

namespace SegoAndBux.Tests.Controllers
{
    public class ProductReviewControllerTests
    {
        private static ProductReviewController Build(IProductReviewService svc, int userId = 101)
        {
            var c = new ProductReviewController(svc);
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "test"))
            };
            c.ControllerContext = new ControllerContext { HttpContext = http };
            return c;
        }

        [Fact]
        public async Task Create_FirstTimeReview_ReturnsOk()
        {
            var svc = new Mock<IProductReviewService>();
            svc.Setup(s => s.HasReviewedAsync(101, 500, 201)).ReturnsAsync(false);
            svc.Setup(s => s.AddReviewAsync(101, It.IsAny<CreateProductReviewDto>()))
               .ReturnsAsync(new ProductReviewDto { ReviewID = 1, ProductID = 201, UserID = 101, Status = "Pending" });

            var sut = Build(svc.Object);

            var res = await sut.Create(new CreateProductReviewDto { OrderID = 500, ProductID = 201, Rating = 5, ReviewText = "Nice" });

            res.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_DuplicateReview_ReturnsBadRequest()
        {
            var svc = new Mock<IProductReviewService>();
            svc.Setup(s => s.HasReviewedAsync(101, 500, 201)).ReturnsAsync(true);

            var sut = Build(svc.Object);

            var res = await sut.Create(new CreateProductReviewDto { OrderID = 500, ProductID = 201, Rating = 5, ReviewText = "dup" });

            res.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task HasReviewed_ReturnsTrue_WhenExists()
        {
            var svc = new Mock<IProductReviewService>();
            svc.Setup(s => s.HasReviewedAsync(101, 500, 201)).ReturnsAsync(true);

            var sut = Build(svc.Object);

            var res = await sut.HasReviewed(500, 201) as OkObjectResult;

            res!.Value.Should().BeOfType<bool>().Which.Should().BeTrue();
        }
    }
}
