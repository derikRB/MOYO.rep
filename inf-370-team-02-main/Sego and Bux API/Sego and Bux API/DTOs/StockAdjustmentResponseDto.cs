namespace Sego_and__Bux.DTOs
{
    public class StockAdjustmentResponseDto
    {
        public int StockAdjustmentId { get; set; }
        public int ProductId { get; set; }
        public int AdjustmentQty { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string AdjustedBy { get; set; } = string.Empty;
        public DateTime AdjustmentDate { get; set; }
    }

}
