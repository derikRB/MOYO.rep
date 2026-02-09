namespace Sego_and__Bux.DTOs
{
    public class DeliveryCalcDto
    {
        public double Distance { get; set; }
        public string DeliveryMethod { get; set; } = "Company Delivery";
        public decimal ShippingFee { get; set; } = 0m;
    }
}
