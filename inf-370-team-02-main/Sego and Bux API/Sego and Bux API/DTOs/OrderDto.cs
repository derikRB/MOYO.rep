using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    public class OrderDto
    {
        public int CustomerID { get; set; }
        public int OrderStatusID { get; set; }
        public decimal TotalPrice { get; set; }

        public string DeliveryMethod { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? CourierProvider { get; set; }

        public List<OrderLineDto> OrderLines { get; set; } = new();
        public DateTime? ExpectedDeliveryDate { get; set; }

    }

    public class OrderLineDto
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public string? Template { get; set; }
        public string? CustomText { get; set; }
        public string? Font { get; set; }
        public int? FontSize { get; set; }
        public string? Color { get; set; }
        public string? UploadedImagePath { get; set; }

    }
}

