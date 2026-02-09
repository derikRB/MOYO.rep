public class OrderResponseDto
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSurname { get; set; } = string.Empty;  // NEW
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;    // NEW

    public int OrderStatusID { get; set; }
    public string OrderStatusName { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }

    public string DeliveryMethod { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? CourierProvider { get; set; }

    public string DeliveryStatus { get; set; } = string.Empty;
    public string? WaybillNumber { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    public List<OrderLineResponseDto> OrderLines { get; set; } = new();
}

// OrderLineResponseDto (used inside OrderResponseDto)
public class OrderLineResponseDto
{
    public int OrderID { get; set; }             // for convenience on the client
    public int OrderLineID { get; set; }
    public int? ProductID { get; set; }          // nullable to match OrderLine.ProductID (soft-deletes)
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }

    public string? Template { get; set; }
    public string? CustomText { get; set; }
    public string? Font { get; set; }
    public int? FontSize { get; set; }
    public string? Color { get; set; }

    public string? UploadedImagePath { get; set; }
    public string? SnapshotPath { get; set; }
    public string? ProductImageUrl { get; set; }
}
