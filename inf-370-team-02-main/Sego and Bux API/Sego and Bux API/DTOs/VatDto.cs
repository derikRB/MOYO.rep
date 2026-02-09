namespace Sego_and__Bux.Dto
{
    public class VatDto
    {
        public string VatName { get; set; } = null!;
        public decimal Percentage { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
}
