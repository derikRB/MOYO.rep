using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        Task<double> CalculateDistanceAsync(string address);

        // ✨ Add this method signature!
        OrderResponseDto BuildOrderResponseDto(Order full);
    }
}
//hhftyg
