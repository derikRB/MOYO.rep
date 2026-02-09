using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Hubs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services;
using SegoAndBux.Tests.Common;
using SegoAndBux.Tests.Common.Builders;
using SegoAndBux.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SegoAndBux.Tests.Services
{
    public class OrderServiceTests
    {
        private static OrderService Build(ApplicationDbContext ctx, FakeHubContext<StockHub> stock, FakeHubContext<MetricsHub> metrics, IAuditWriter audit)
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string,string>("Google:ApiKey","X"),
                    new System.Collections.Generic.KeyValuePair<string,string>("Google:CompanyAddress","HQ")
                })
                .Build();

            return new OrderService(cfg, ctx, stock, metrics, audit);
        }

        private static void SeedBasics(ApplicationDbContext ctx)
        {
            ctx.OrderStatuses.Add(new OrderStatus { OrderStatusID = 1, OrderStatusName = "Pending" });

            // ✅ Fill ALL required Customer fields so EF InMemory can save
            ctx.Customers.Add(new Customer
            {
                CustomerID = 101,
                Username = "cust1",
                Email = "cust1@example.com",
                IsVerified = true,
                Name = "Test",
                Surname = "User",
                Phone = "0000000000",
                Address = "123 Test St",
                PasswordHash = "hashed" // any non-null string is OK for tests
            });

            var prod1 = new ProductBuilder()
                .WithId(201).WithName("Pink Tee").WithPrice(100m).WithStock(10).WithPrimaryImage("pinktee.jpg")
                .Build();
            var prod2 = new ProductBuilder()
                .WithId(202).WithName("Blue Hoodie").WithPrice(300m).WithStock(5).WithPrimaryImage("bluehoodie.jpg")
                .Build();

            ctx.Products.AddRange(prod1, prod2);
            ctx.SaveChanges();
        }

        [Fact]
        public async Task PlaceOrder_Valid_Persists_DecrementsStock_EmitsSignals_MapsDto()
        {
            // Arrange
            var db = TestDb.NewContext();
            SeedBasics(db);

            var stock = new FakeHubContext<StockHub>();
            var metrics = new FakeHubContext<MetricsHub>();
            var audit = new FakeAuditWriter();
            var sut = Build(db, stock, metrics, audit);

            var dto = new OrderBuilder().Build();

            // Act
            var result = await sut.PlaceOrderAsync(dto);

            // Assert — persistence
            db.Orders.Count().Should().Be(1);
            db.OrderLines.Count().Should().Be(2);

            // Assert — stock math
            (await db.Products.FirstAsync(p => p.ProductID == 201)).StockQuantity.Should().Be(8);
            (await db.Products.FirstAsync(p => p.ProductID == 202)).StockQuantity.Should().Be(4);

            // Assert — DTO image URL + lines
            result.OrderLines.Should().HaveCount(2);
            result.OrderLines.Select(l => l.ProductImageUrl).Should().OnlyContain(u => u!.StartsWith("/images/products/"));

            // Assert — hubs & audit
            metrics.ClientsImpl.AllCalls.Should().NotBeEmpty();
            audit.Calls.Should().ContainSingle(c => c.Action == "Create" && c.Entity == "Order");
        }

        [Fact]
        public async Task PlaceOrder_MissingProduct_ThrowsInvalidOperationException()
        {
            var db = TestDb.NewContext();
            SeedBasics(db);
            var sut = Build(db, new FakeHubContext<StockHub>(), new FakeHubContext<MetricsHub>(), new FakeAuditWriter());

            var dto = new OrderDto
            {
                CustomerID = 101,
                OrderStatusID = 1,
                TotalPrice = 100m,
                DeliveryMethod = "Courier",
                DeliveryAddress = "X",
                OrderLines = { new OrderLineDto { ProductID = 999, Quantity = 1 } }
            };

            var act = async () => await sut.PlaceOrderAsync(dto);
            await act.Should().ThrowAsync<System.InvalidOperationException>()
                .WithMessage("*ProductIDs do not exist*");
        }

        [Fact]
        public async Task PlaceOrder_SavesCustomization_WhenProvided()
        {
            var db = TestDb.NewContext();
            SeedBasics(db);
            var sut = Build(db, new FakeHubContext<StockHub>(), new FakeHubContext<MetricsHub>(), new FakeAuditWriter());

            var dto = new OrderDto
            {
                CustomerID = 101,
                OrderStatusID = 1,
                TotalPrice = 100m,
                DeliveryMethod = "Courier",
                DeliveryAddress = "X",
                OrderLines = { new OrderLineDto { ProductID = 201, Quantity = 1, Template = "T1", CustomText = "Hi", Font = "Arial", Color = "#000" } }
            };

            var res = await sut.PlaceOrderAsync(dto);

            db.Customizations.Count().Should().Be(1);
            res.OrderLines[0].CustomText.Should().Be("Hi");
        }
    }
}
