using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sego_and__Bux.Data;
using Sego_and__Bux.Hubs;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services;
using SegoAndBux.Tests.Common;
using SegoAndBux.Tests.Common.Builders;
using SegoAndBux.Tests.Fakes;
using Xunit;

namespace SegoAndBux.Tests.Intergration
{
    public class PlaceOrder_IntegrationTests
    {
        private static OrderService Build(ApplicationDbContext ctx)
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string,string>("Google:ApiKey","X"),
                    new KeyValuePair<string,string>("Google:CompanyAddress","HQ")
                })
                .Build();

            return new OrderService(cfg, ctx, new FakeHubContext<StockHub>(), new FakeHubContext<MetricsHub>(), new FakeAuditWriter());
        }

        private static void Seed(ApplicationDbContext ctx)
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
                PasswordHash = "hashed"
            });

            ctx.Products.Add(new ProductBuilder()
                .WithId(201).WithName("Pink Tee").WithPrice(100m).WithStock(10).WithPrimaryImage("pinktee.jpg")
                .Build());
            ctx.Products.Add(new ProductBuilder()
                .WithId(202).WithName("Blue Hoodie").WithPrice(300m).WithStock(5).WithPrimaryImage("bluehoodie.jpg")
                .Build());

            ctx.SaveChanges();
        }

        [Fact]
        public async Task PlaceOrder_EndToEnd_PersistsAndCanBeReQueried()
        {
            // Unique DB name so the second context can re-open same store
            var dbName = Guid.NewGuid().ToString("N");
            using var ctx = TestDb.NewContext(dbName);
            Seed(ctx);
            var svc = Build(ctx);

            var dto = new OrderBuilder().Build();

            // Act
            var returned = await svc.PlaceOrderAsync(dto);

            // Assert via NEW context instance (simulates another request)
            using var verify = TestDb.NewContext(dbName);
            var saved = await verify.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .FirstOrDefaultAsync(o => o.OrderID == returned.OrderID);

            saved.Should().NotBeNull();
            saved!.OrderLines.Should().HaveCount(2);
            saved.Customer!.Email.Should().Be("cust1@example.com");
        }
    }
}
