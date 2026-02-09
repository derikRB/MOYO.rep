using System;
using System.Collections.Generic;
using Sego_and__Bux.DTOs;

namespace SegoAndBux.Tests.Common.Builders
{
    public sealed class OrderBuilder
    {
        private readonly OrderDto _dto;

        public OrderBuilder()
        {
            _dto = new OrderDto
            {
                CustomerID = 101,
                OrderStatusID = 1,
                TotalPrice = 500m,
                DeliveryMethod = "Courier",
                DeliveryAddress = "123 Test St",
                CourierProvider = "FastShip",
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
                OrderLines = new List<OrderLineDto>
                {
                    new() { ProductID = 201, Quantity = 2, Template = "T1", CustomText = "Hi" },
                    new() { ProductID = 202, Quantity = 1 }
                }
            };
        }

        public OrderBuilder WithCustomer(int id) { _dto.CustomerID = id; return this; }
        public OrderBuilder WithTotal(decimal total) { _dto.TotalPrice = total; return this; }
        public OrderBuilder WithLines(params OrderLineDto[] lines)
        {
            _dto.OrderLines = new List<OrderLineDto>(lines);
            return this;
        }

        public OrderDto Build() => _dto;
    }
}
