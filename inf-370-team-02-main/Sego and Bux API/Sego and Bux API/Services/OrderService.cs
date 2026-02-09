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

        public OrderService(IConfiguration cfg, ApplicationDbContext ctx, IHubContext<StockHub> stockHub)
        {
            _ctx = ctx;
            _apiKey = cfg["Google:ApiKey"] ?? throw new ArgumentNullException(nameof(cfg));
            _companyAddr = cfg["Google:CompanyAddress"] ?? throw new ArgumentNullException(nameof(cfg));
            _stockHub = stockHub;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(OrderDto dto)
        {
            // --- BULLETPROOF PRODUCT CHECK ---
            var allProductIds = dto.OrderLines.Select(l => l.ProductID).ToList();
            var productsInDb = await _ctx.Products.Where(p => allProductIds.Contains(p.ProductID)).ToListAsync();
            var missingIds = allProductIds.Except(productsInDb.Select(p => p.ProductID)).ToList();

            if (missingIds.Any())
                throw new InvalidOperationException($"Invalid order: The following ProductIDs do not exist: {string.Join(", ", missingIds)}. Please clear your cart and try again.");

            // --- CREATE ORDER ---
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

            // --- CREATE ORDER LINES & DECREMENT STOCK ---
            foreach (var l in dto.OrderLines)
            {
                var prod = productsInDb.First(p => p.ProductID == l.ProductID);

                var ol = new OrderLine
                {
                    ProductID = l.ProductID,
                    Quantity = l.Quantity
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

            // --- GUARANTEED CUSTOMIZATIONS: Always create a Customization row for every OrderLine ---
            foreach (var l in dto.OrderLines)
            {
                var ol = order.OrderLines.FirstOrDefault(x => x.ProductID == l.ProductID);
                if (ol == null) continue;

                var customization = new Customization
                {
                    OrderLineID = ol.OrderLineID,
                    Template = l.Template ?? "",
                    CustomText = l.CustomText ?? "",
                    Font = l.Font ?? "Arial",
                    FontSize = l.FontSize ?? 16,
                    Color = l.Color ?? "#000000",
                    UploadedImagePath = l.UploadedImagePath ?? ""
                };
                _ctx.Customizations.Add(customization);
            }

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save order: " + ex.Message, ex);
            }

            // --- BROADCAST STOCK UPDATES ---
            foreach (var ol in order.OrderLines)
            {
                var prod = await _ctx.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == ol.ProductID);
                if (prod != null)
                {
                    var update = new ProductStockUpdateDto
                    {
                        ProductID = prod.ProductID,
                        StockQuantity = prod.StockQuantity
                    };
                    await _stockHub.Clients.All.SendAsync("ReceiveStockUpdate", update);
                }
            }

            // --- BUILD RESPONSE DTO ---
            var full = await _ctx.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (full == null)
                throw new Exception("Order not found after saving.");

            return BuildOrderResponseDto(full);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            var all = await _ctx.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
                .ToListAsync();

            return all.Select(BuildOrderResponseDto);
        }

        public OrderResponseDto BuildOrderResponseDto(Order full)
        {
            var customer = full.Customer;
            return new OrderResponseDto
            {
                OrderID = full.OrderID,
                CustomerID = full.CustomerID,
                CustomerName = customer?.Name ?? "",
                CustomerSurname = customer?.Surname ?? "",
                CustomerEmail = customer?.Email ?? "",
                CustomerPhone = customer?.Phone ?? "",
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
                OrderLines = full.OrderLines.Select(ol => new OrderLineResponseDto
                {
                    ProductID = ol.ProductID,
                    OrderLineID = ol.OrderLineID,
                    ProductName = ol.Product?.Name ?? "",
                    Quantity = ol.Quantity,
                    Template = ol.Customization?.Template,
                    CustomText = ol.Customization?.CustomText,
                    Font = ol.Customization?.Font,
                    FontSize = ol.Customization?.FontSize,
                    Color = ol.Customization?.Color,
                    UploadedImagePath = ol.Customization?.UploadedImagePath,
                    SnapshotPath = ol.Customization?.SnapshotPath,
                    ProductImageUrl = ol.Product?.PrimaryImage?.Url
                }).ToList()
            };
        }

        // The rest remain unchanged
        public Task<Order?> GetOrderByIdAsync(int orderId)
            => _ctx.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderLines).ThenInclude(ol => ol.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

        public Task<IEnumerable<Order>> GetOrdersByCustomerAsync(int cid)
         => _ctx.Orders
             .Where(o => o.CustomerID == cid)
             .Include(o => o.Customer)
             .Include(o => o.OrderStatus)
             .Include(o => o.OrderLines).ThenInclude(ol => ol.Product)
             .Include(o => o.OrderLines).ThenInclude(ol => ol.Customization)
             .ToListAsync()
             .ContinueWith(t => (IEnumerable<Order>)t.Result);

        public async Task<bool> CancelOrderAsync(int id)
        {
            var o = await _ctx.Orders.FindAsync(id);
            if (o == null) return false;
            o.OrderStatusID = 3; // Cancelled
            await _ctx.SaveChangesAsync();
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
            return o;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, int newStatusId)
        {
            var o = await _ctx.Orders.FindAsync(id);
            if (o == null) return false;
            o.OrderStatusID = newStatusId;
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDeliveryInfoAsync(int id, DeliveryUpdateDto dto)
        {
            var o = await _ctx.Orders.FindAsync(id);
            if (o == null) return false;
            o.DeliveryStatus = dto.DeliveryStatus;
            o.WaybillNumber = dto.WaybillNumber;
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<double> CalculateDistanceAsync(string address)
        {
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json" +
                      $"?origins={Uri.EscapeDataString(_companyAddr)}" +
                      $"&destinations={Uri.EscapeDataString(address)}" +
                      $"&key={_apiKey}";

            using var http = new HttpClient();
            var json = await http.GetStringAsync(url);
            dynamic r = JsonConvert.DeserializeObject(json)!;
            return (double)r.rows[0].elements[0].distance.value / 1000.0;
        }

        public async Task<bool> UpdateExpectedDeliveryDateAsync(int orderId, DateTime newDate)
        {
            var order = await _ctx.Orders.FindAsync(orderId);
            if (order == null) return false;
            order.ExpectedDeliveryDate = newDate;
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
