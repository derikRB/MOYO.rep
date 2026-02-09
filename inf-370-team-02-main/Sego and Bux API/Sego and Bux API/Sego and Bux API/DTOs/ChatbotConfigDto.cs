namespace Sego_and__Bux.DTOs
{
    public class ChatbotConfigDto
    {
        public int Id { get; set; }
        public string WhatsAppNumber { get; set; } = "";
        public string SupportEmail { get; set; } = "";

        // NEW – surfaced to Admin UI and used by OrderController
        public string CompanyAddress { get; set; } = "";
        public int ThresholdKm { get; set; } = 20;     // maps to DeliveryRadiusKm
        public decimal FlatShippingFee { get; set; } = 100m;  // maps to CourierFlatFee
        public decimal HandToHandFee { get; set; } = 0m;    // maps to HandToHandFee
    }
}
