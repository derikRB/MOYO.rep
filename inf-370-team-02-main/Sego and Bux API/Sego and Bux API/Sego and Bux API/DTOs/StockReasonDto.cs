// DTOs/StockReasonDto.cs
namespace Sego_and__Bux.DTOs
{
    public class StockReasonDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }

    public class StockReasonResponseDto
    {
        public int StockReasonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
