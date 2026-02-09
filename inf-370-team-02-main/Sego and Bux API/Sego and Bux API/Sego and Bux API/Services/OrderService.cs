using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Microsoft.AspNetCore.SignalR;
using Sego_and__Bux.Hubs;

namespace Sego_and__Bux.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly string _apiKey, _companyAddr;
        private readonly IHubContext<StockHub> _stockHub;
        private readonly IHubContext<MetricsHub> _metricsHub;
        private readonly IAuditWriter _audit;

        public OrderService(
            IConfiguration cfg,
            ApplicationDbContext ctx,
            IHubContext<StockHub> stockHub,
            IHubContext<MetricsHub> metricsHub,
            IAuditWriter audit)
        {
            _ctx = ctx;
            _apiKey = cfg["Google:ApiKey"] ?? throw new ArgumentNullException(nameof(cfg));
            _companyAddr = cfg["Google:CompanyAddress"] ?? throw new ArgumentNullException(nameof(cfg));
            _stockHub = stockHub;
            _metricsHub = metricsHub;
            _audit = audit;
        }

        // -------------------- POLICY HELPERS --------------------
        private async Task<ChatbotConfig> LoadPolicyAsync()
        {
            var p = await _ctx.ChatbotConfigs.AsNoTracking().FirstOrDefaultAsync();
            if (p == null)
            {
                p = new ChatbotConfig
                {
                    CompanyAddress = _companyAddr,
                    DeliveryRadiusKm = 20,
                    CourierFlatFee = 100m,
                    HandToHandFee = 0m
                };
            }

            if (string.IsNullOrWhiteSpace(p.CompanyAddress))
                p.CompanyAddress = _companyAddr;

            return p;
        }

        private async Task<double> DistanceFromOriginAsync(string origin, string address)
        {
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json" +
                      $"?origins={Uri.EscapeDataString(origin)}" +
                      $"&destinations={Uri.EscapeDataString(address)}" +
                      $"&key={_apiKey}";

            using var http = new HttpClient();
            var json = await http.GetStringAsync(url);
            dynamic r = JsonConvert.DeserializeObject(json)!;
            return (double)r.rows[0].elements[0].distance.value / 1000.0;
        }

        public async Task<DeliveryCalcDto> CalculateDeliveryAsync(string destinationAddress)
        {
            var policy = await LoadPolicyAsync();
            var km = await DistanceFromOriginAsync(policy.CompanyAddress, destinationAddress);

            var dto = new DeliveryCalcDto { Distance = km };
            if (km > policy.DeliveryRadiusKm)
            {
                dto.DeliveryMethod = "Courier";
                dto.ShippingFee = policy.CourierFlatFee;
            }
            else
            {
                dto.DeliveryMethod = "Company Delivery";
                dto.ShippingFee = policy.HandToHandFee;
            }
            return dto;
        }

        public async Task<double> CalculateDistanceAsync(string address)
        {
            var origin = await _ctx.ChatbotConfigs.AsNoTracking()
                .Select(c => c.CompanyAddress)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(origin))
                origin = _companyAddr;

            return await DistanceFromOriginAsync(origin, address);
        }

        // -------------------- CORE ORDER WORKFLOW --------------------
        public async Task<OrderResponseDto> PlaceOrderAsync(OrderDto dto)
        {
            var allProductIds = dto.OrderLines.Select(l => l.ProductID).ToList();

            var productsInDb = await _ctx.Products
                .Where(p => allProductIds.Contains(p.ProductID))
                .Include(p => p.PrimaryImage)
                .ToListAsync();

            var missingIds = allProductIds.Except(productsInDb.Select(p => p.ProductID)).ToList();
            if (missingIds.Count > 0)
                throw new InvalidOperationException($"Invalid order: The following ProductIDs do not exist: {string.Join(", ", missingIds)}. Please clear your cart and try again.");

            var order = new Order
            {
                CustomerID = dto.CustomerID,
                OrderStatusID = dto.OrderStatusID,
                TotalPrice = dto.TotalPrice,
                OrderDate = DateTime.UtcNow,
                DeliveryMethod = dto.DeliveryMethod,
                DeliveryAddress = dto.DeliveryAddress,
                CourierProvider = dto.CourierProvider,
                DeliveryStatus = "Pending",
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate ?? DateTime.UtcNow.AddDays(10)
            };

            foreach (var l in dto.OrderLines)
            {
                var prod = productsInDb.First(p => p.ProductID == l.ProductID);

                var ol = new OrderLine
                {
                    ProductID = l.ProductID,
                    Quantity = l.Quantity,
                    ProductNameSnapshot = prod.Name,
                    ProductImagePathSnapshot = prod.PrimaryImage?.ImagePath
                };
                order.OrderLines.Add(ol);

                prod.StockQuantity -= l.Quantity;
                if (prod.StockQuantity < 0) prod.StockQuantity = 0;

                _ctx.StockTransactionEntities.Add(new StockTransactionEntity
                {
                    ProductID = prod.ProductID,
                    TranDate = DateTime.UtcNow,
                    Received = null,
                    Adjusted = -l.Quantity
                });
            }

            _ctx.Orders.Add(order);
            await _ctx.SaveChangesAsync();

            foreach (var l in dto.OrderLines)
            {
                var ol = order.OrderLines.FirstOrDefault(x => x.ProductID == l.ProductID);
                if (ol == null) continue;

                _ctx.Customizations.Add(new Customization
                {
                    OrderLineID = ol.OrderLineID,
                    Template = l.Template ?? "",
                    CustomText = l.CustomText ?? "",
                    Font = l.Font ?? "Arial",
                    FontSize = l.FontSize ?? 16,
                    Color = l.Color ?? "#000000",
                    UploadedImagePath = l.UploadedImagePath ?? ""
                });
            }

            await _ctx.SaveChangesAsync();

            foreach (var ol in order.OrderLines)
            {
                if (ol.ProductID == null) continue;
                var prod = await _ctx.Products.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductID == ol.ProductID.Value);
                if (prod != null)
                {
                    await _stockHub.Clients.All.SendAsync("StockUpdated", new ProductStockUpdateDto
                    {
                        ProductID = prod.ProductID,
                        StockQuantity = prod.StockQuantity
                    });
                }
            }

            await _metricsHub.Clients.All.SendAsync("salesChanged");
            await _metricsHub.Clients.All.SendAsync("orderStatusChanged");
            await _metricsHub.Clients.All.SendAsync("inventoryChanged");

            await _audit.WriteAsync(
                action: "Create",
                entity: "Order",
                entityId: order.OrderID.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    order.OrderID,
                    order.CustomerID,
                    order.TotalPrice,
                    Items = order.OrderLines.Count
                }),
                criticalValue: $"Total={order.TotalPrice:F2}, Items={order.OrderLines.Count}"
            );

            var full = await _ctx.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product).ThenInclude(p => p.PrimaryImage)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID)
                ?? throw new Exception("Order not found after saving.");

            return BuildOrderResponseDto(full);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            var all = await _ctx.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product).ThenInclude(p => p.PrimaryImage)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .ToListAsync();

            return all.Select(BuildOrderResponseDto);
        }

        public Task<Order?> GetOrderByIdAsync(int orderId) =>
            _ctx.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product).ThenInclude(p => p.PrimaryImage)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

        public Task<IEnumerable<Order>> GetOrdersByCustomerAsync(int cid) =>
            _ctx.Orders
                .Where(o => o.CustomerID == cid)
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product).ThenInclude(p => p.PrimaryImage)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<Order>)t.Result);

        public async Task<bool> CancelOrderAsync(int id)
        {
            var o = await _ctx.Orders.FindAsync(id);
            if (o == null) return false;
            o.OrderStatusID = 3;
            await _ctx.SaveChangesAsync();

            await _audit.WriteAsync(
                "Update", "OrderStatus", id.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { OrderID = id, NewStatusId = 3 }),
                criticalValue: "StatusId=3"
            );

            return true;
        }

        public async Task<Order?> UpdateOrderAsync(int id, OrderDto dto)
        {
            var o = await _ctx.Orders
                .Include(x => x.OrderLines)
                .FirstOrDefaultAsync(x => x.OrderID == id);
            if (o == null || o.OrderStatusID != 1) return null;

            o.TotalPrice = dto.TotalPrice;
            o.DeliveryMethod = dto.DeliveryMethod;
            o.DeliveryAddress = dto.DeliveryAddress;
            o.CourierProvider = dto.CourierProvider;

            _ctx.OrderLines.RemoveRange(o.OrderLines);
            o.OrderLines = dto.OrderLines
                .Select(l => new OrderLine { ProductID = l.ProductID, Quantity = l.Quantity })
                .ToList();

            await _ctx.SaveChangesAsync();

            await _audit.WriteAsync(
                "Update", "Order", id.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { o.OrderID, o.TotalPrice }),
                criticalValue: $"Total={o.TotalPrice:F2}"
            );

            return o;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, int newStatusId)
        {
            var o = await _ctx.Orders.FindAsync(id);
            if (o == null) return false;
            o.OrderStatusID = newStatusId;
            await _ctx.SaveChangesAsync();

            await _metricsHub.Clients.All.SendAsync("orderStatusChanged");

            await _audit.WriteAsync(
                "Update", "OrderStatus", id.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { OrderID = id, NewStatusId = newStatusId }),
                criticalValue: $"StatusId={newStatusId}"
            );

            return true;
        }

        public async Task<bool> UpdateDeliveryInfoAsync(int id, DeliveryUpdateDto dto)
        {
            var o = await _ctx.Orders.FindAsync(id);
            if (o == null) return false;
            o.DeliveryStatus = dto.DeliveryStatus;
            o.WaybillNumber = dto.WaybillNumber;
            await _ctx.SaveChangesAsync();

            await _audit.WriteAsync(
                "Update", "OrderDelivery", id.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(dto),
                criticalValue: $"DeliveryStatus={dto.DeliveryStatus}"
            );

            return true;
        }

        public async Task<bool> UpdateExpectedDeliveryDateAsync(int orderId, DateTime newDate)
        {
            var order = await _ctx.Orders.FindAsync(orderId);
            if (order == null) return false;
            order.ExpectedDeliveryDate = newDate;
            await _ctx.SaveChangesAsync();

            await _audit.WriteAsync(
                "Update", "ExpectedDeliveryDate", orderId.ToString(),
                beforeJson: null,
                afterJson: System.Text.Json.JsonSerializer.Serialize(new { orderId, newDate }),
                criticalValue: $"ExpectedDate={newDate:yyyy-MM-dd}"
            );

            return true;
        }

        // In the BuildOrderResponseDto method, update the customer handling:
        public OrderResponseDto BuildOrderResponseDto(Order full)
        {
            static string? ToProductsUrl(string? pathOrUrl)
            {
                if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;
                return pathOrUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                    ? pathOrUrl
                    : "/images/products/" + pathOrUrl;
            }

            var customer = full.Customer;

            // 🔑 Handle deleted customers gracefully while preserving order data
            var isDeleted = customer?.IsDeleted == true;
            var custName = isDeleted ? "Deleted Customer" : customer?.Name ?? "Unknown";
            var custSurname = isDeleted ? "" : customer?.Surname ?? "";
            var custEmail = isDeleted ? "deleted@example.com" : customer?.Email ?? "";
            var custPhone = isDeleted ? "0000000000" : customer?.Phone ?? "";

            return new OrderResponseDto
            {
                OrderID = full.OrderID,
                CustomerID = full.CustomerID,
                CustomerName = custName,
                CustomerSurname = custSurname,
                CustomerEmail = custEmail,
                CustomerPhone = custPhone,
                OrderStatusID = full.OrderStatusID,
                OrderStatusName = full.OrderStatus?.OrderStatusName ?? "",
                TotalPrice = full.TotalPrice,
                OrderDate = full.OrderDate,
                DeliveryMethod = full.DeliveryMethod ?? "",
                DeliveryAddress = full.DeliveryAddress ?? "",
                CourierProvider = full.CourierProvider,
                DeliveryStatus = full.DeliveryStatus ?? "",
                WaybillNumber = full.WaybillNumber,
                ExpectedDeliveryDate = full.ExpectedDeliveryDate,
                OrderLines = full.OrderLines.Select(ol =>
                {
                    var liveName = ol.Product?.Name;
                    var snapName = ol.ProductNameSnapshot;
                    var liveImg = ol.Product?.PrimaryImage?.ImagePath;
                    var snapImg = ol.ProductImagePathSnapshot;
                    var chosen = liveImg ?? snapImg;
                    var url = chosen != null ? ToProductsUrl(chosen) : null;

                    return new OrderLineResponseDto
                    {
                        OrderID = full.OrderID,
                        OrderLineID = ol.OrderLineID,
                        ProductID = ol.ProductID,
                        ProductName = liveName ?? snapName ?? "",
                        Quantity = ol.Quantity,
                        Template = ol.Customization?.Template,
                        CustomText = ol.Customization?.CustomText,
                        Font = ol.Customization?.Font,
                        FontSize = ol.Customization?.FontSize,
                        Color = ol.Customization?.Color,
                        UploadedImagePath = ol.Customization?.UploadedImagePath,
                        SnapshotPath = ol.Customization?.SnapshotPath,
                        ProductImageUrl = url
                    };
                }).ToList()
            };
        }
    }
}