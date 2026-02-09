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

        public OrderController(IOrderService orderSvc, IEmailService emailSvc)
        {
            _orderSvc = orderSvc;
            _emailSvc = emailSvc;
        }

        [HttpPost("place"), Authorize(Roles = "Customer")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDto dto)
        {
            if (dto == null || dto.CustomerID <= 0 || !dto.OrderLines.Any())
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

        [HttpPost("calculateDelivery"), AllowAnonymous]
        public async Task<IActionResult> Calculate([FromBody] AddressDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Address))
                return BadRequest("Address is required.");

            try
            {
                var dist = await _orderSvc.CalculateDistanceAsync(dto.Address);
                var method = dist <= 20 ? "Hand-to-Hand" : "Courier";
                var shippingFee = method == "Courier" ? 100 : 0;
                return Ok(new { DeliveryMethod = method, Distance = dist, ShippingFee = shippingFee });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Delivery calculation failed", error = ex.Message });
            }
        }

        [HttpGet("all"), Authorize(Roles = "Employee,Manager,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var allOrders = await _orderSvc.GetAllOrdersAsync();
            return Ok(allOrders);
        }

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
                        ToName = order.Customer!.Name + " " + order.Customer.Surname,
                        ToEmail = order.Customer.Email,
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
