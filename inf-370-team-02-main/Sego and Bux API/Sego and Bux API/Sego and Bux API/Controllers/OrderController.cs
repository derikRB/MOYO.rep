using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderSvc;
        private readonly IEmailService _emailSvc;
        private readonly IChatbotConfigService _chatbotConfigService;

        public OrderController(
            IOrderService orderSvc,
            IEmailService emailSvc,
            IChatbotConfigService chatbotConfigService)
        {
            _orderSvc = orderSvc;
            _emailSvc = emailSvc;
            _chatbotConfigService = chatbotConfigService;
        }

        [HttpPost("place"), Authorize(Roles = "Customer")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDto dto)
        {
            if (dto == null || dto.CustomerID <= 0 || dto.OrderLines.Count == 0)
                return BadRequest("Invalid order data.");

            OrderResponseDto placed;
            try
            {
                placed = await _orderSvc.PlaceOrderAsync(dto);
            }
            catch (InvalidOperationException invEx)
            {
                return BadRequest(new { message = invEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to place order", error = ex.Message });
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSvc.SendEmailAsync(new EmailDto
                    {
                        ToName = placed.CustomerName,
                        ToEmail = placed.CustomerEmail,
                        Subject = $"Order #{placed.OrderID} Placed",
                        Message = $"Thank you for your order! Total: R{placed.TotalPrice:N2}"
                    });
                }
                catch { }
            });

            return Ok(placed);
        }

        // Uses the dynamic shipping policy in ChatbotConfigs
        [HttpPost("calculateDelivery"), AllowAnonymous]
        public async Task<IActionResult> Calculate([FromBody] AddressDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Address is required.");

            try
            {
                // Distance from DB-origin (with appsettings fallback inside service)
                var distKm = await _orderSvc.CalculateDistanceAsync(dto.Address);

                // Pull policy (in case UI wants to display the rule numbers)
                var policy = await _chatbotConfigService.GetAsync();
                var thresholdKm = policy?.ThresholdKm ?? 20;
                var courierFlat = policy?.FlatShippingFee ?? 100m;
                var handToHand = policy?.HandToHandFee ?? 0m;

                var method = distKm > thresholdKm ? "Courier" : "Company Delivery";
                var fee = distKm > thresholdKm ? courierFlat : handToHand;

                return Ok(new
                {
                    deliveryMethod = method,
                    distance = distKm,
                    shippingFee = fee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Delivery calculation failed", error = ex.Message });
            }
        }

        [HttpGet("all"), Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetAll() =>
            Ok(await _orderSvc.GetAllOrdersAsync());

        [HttpGet("customer/{customerId}"), Authorize(Roles = "Customer")]
        public async Task<IActionResult> ByCustomer(int customerId)
        {
            var orders = await _orderSvc.GetOrdersByCustomerAsync(customerId);
            var dtos = orders.Select(o => _orderSvc.BuildOrderResponseDto(o)).ToList();
            return Ok(dtos);
        }

        [HttpPut("{orderId}"), Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(int orderId, [FromBody] OrderDto dto)
        {
            var updated = await _orderSvc.UpdateOrderAsync(orderId, dto);
            return updated == null ? BadRequest("Cannot update this order.") : Ok("Updated");
        }

        [HttpPut("{orderId}/cancel"), Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int orderId) =>
            Ok(await _orderSvc.CancelOrderAsync(orderId));

        [HttpPut("{orderId}/status/{newStatusId}"), Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> Status(int orderId, int newStatusId)
        {
            var ok = await _orderSvc.UpdateOrderStatusAsync(orderId, newStatusId);
            if (!ok) return NotFound("Order not found.");

            _ = Task.Run(async () =>
            {
                try
                {
                    var order = await _orderSvc.GetOrderByIdAsync(orderId);
                    if (order?.OrderStatus == null) return;

                    await _emailSvc.SendEmailAsync(new EmailDto
                    {
                        ToName = order.Customer!.Name + " " + order.Customer!.Surname,
                        ToEmail = order.Customer!.Email,
                        Subject = $"Order #{orderId} Status Updated",
                        Message = $"Your order status is now: {order.OrderStatus.OrderStatusName}."
                    });
                }
                catch { }
            });

            return Ok(new { message = "Status updated." });
        }

        [HttpGet("{orderId}"), Authorize(Roles = "Employee,Manager,Admin,Customer")]
        public async Task<IActionResult> GetById(int orderId)
        {
            var order = await _orderSvc.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            var dto = _orderSvc.BuildOrderResponseDto(order);
            return Ok(dto);
        }

        [HttpPut("{orderId}/delivery"), Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> Delivery(int orderId, [FromBody] DeliveryUpdateDto dto)
        {
            var ok = await _orderSvc.UpdateDeliveryInfoAsync(orderId, dto);
            if (!ok) return NotFound("Order not found.");

            _ = Task.Run(async () =>
            {
                try
                {
                    var order = await _orderSvc.GetOrderByIdAsync(orderId);
                    if (order?.Customer == null) return;

                    await _emailSvc.SendEmailAsync(new EmailDto
                    {
                        ToName = order.Customer.Name + " " + order.Customer.Surname,
                        ToEmail = order.Customer.Email,
                        Subject = $"Order #{orderId} Delivery Updated",
                        Message = $"Delivery status: {dto.DeliveryStatus}\nWaybill: {dto.WaybillNumber ?? "N/A"}"
                    });
                }
                catch { }
            });

            return Ok(new { message = "Delivery updated." });
        }

        [HttpPatch("{orderId}/expectedDeliveryDate"), Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> UpdateExpectedDeliveryDate(int orderId, [FromBody] DateTime expectedDate)
        {
            var ok = await _orderSvc.UpdateExpectedDeliveryDateAsync(orderId, expectedDate);
            if (!ok) return NotFound("Order not found.");
            return Ok("Expected delivery date updated.");
        }
    }
}
