namespace Sego_and__Bux.DTOs
{
    public class DeliveryUpdateDto
    {
        // previously missing
        public string DeliveryStatus { get; set; } = string.Empty;
        public string? WaybillNumber { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; } 
    }
}
