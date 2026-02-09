using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.Controllers;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using SegoAndBux.Tests.Fakes;
using Xunit;

namespace SegoAndBux.Tests.Controllers
{
    public class OrderControllerTests
    {
        [Fact]
        public async Task Status_ExistingOrder_UpdatesAndSendsEmail_ReturnsOk()
        {
            // Arrange
            var orderSvc = new Mock<IOrderService>();
            orderSvc.Setup(s => s.UpdateOrderStatusAsync(10, 3)).ReturnsAsync(true);
            orderSvc.Setup(s => s.GetOrderByIdAsync(10)).ReturnsAsync(new Order
            {
                OrderID = 10,
                Customer = new Customer { Name = "Pink", Surname = "Panther", Email = "cust@x.com" },
                OrderStatus = new OrderStatus { OrderStatusName = "Shipped" }
            });

            var email = new FakeEmailService();
            var sut = new OrderController(orderSvc.Object, email);

            // Act
            var result = await sut.Status(10, 3);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            email.Sent.Should().HaveCount(1);
            email.Sent[0].Subject.Should().Contain("Order #10");
            email.Sent[0].Message.Should().Contain("Shipped");
        }

        [Fact]
        public async Task Status_UnknownOrder_ReturnsNotFound_AndNoEmail()
        {
            var orderSvc = new Mock<IOrderService>();
            orderSvc.Setup(s => s.UpdateOrderStatusAsync(99, 2)).ReturnsAsync(false);

            var email = new FakeEmailService();
            var sut = new OrderController(orderSvc.Object, email);

            var res = await sut.Status(99, 2);

            res.Should().BeOfType<NotFoundObjectResult>();
            email.Sent.Should().BeEmpty();
        }

        [Fact]
        public async Task Calculate_ShortDistance_ReturnsHandToHand_NoFee()
        {
            var svc = new Mock<IOrderService>();
            svc.Setup(s => s.CalculateDistanceAsync("A")).ReturnsAsync(5); // 5km

            var sut = new OrderController(svc.Object, new FakeEmailService());
            var res = await sut.Calculate(new AddressDto { Address = "A" }) as OkObjectResult;

            res!.Value.Should().BeEquivalentTo(new { DeliveryMethod = "Hand-to-Hand", Distance = 5d, ShippingFee = 0 });
        }

        [Fact]
        public async Task Calculate_LongDistance_ReturnsCourier_WithFee()
        {
            var svc = new Mock<IOrderService>();
            svc.Setup(s => s.CalculateDistanceAsync("B")).ReturnsAsync(30); // 30km

            var sut = new OrderController(svc.Object, new FakeEmailService());
            var res = await sut.Calculate(new AddressDto { Address = "B" }) as OkObjectResult;

            res!.Value.Should().BeEquivalentTo(new { DeliveryMethod = "Courier", Distance = 30d, ShippingFee = 100 });
        }
    }
}
