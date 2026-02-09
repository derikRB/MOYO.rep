using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> PlaceOrderAsync(OrderDto dto);

        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByCustomerAsync(int customerId);
        Task<bool> CancelOrderAsync(int orderId);
        Task<Order?> UpdateOrderAsync(int orderId, OrderDto dto);
        Task<bool> UpdateOrderStatusAsync(int orderId, int newStatusId);
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
        Task<bool> UpdateDeliveryInfoAsync(int orderId, DeliveryUpdateDto dto);
        Task<bool> UpdateExpectedDeliveryDateAsync(int orderId, DateTime newDate);

        // Distance & delivery helpers
        Task<double> CalculateDistanceAsync(string address);
        Task<DeliveryCalcDto> CalculateDeliveryAsync(string destinationAddress);

        // Projectors
        OrderResponseDto BuildOrderResponseDto(Order full);
    }
}
