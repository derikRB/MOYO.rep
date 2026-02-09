namespace Sego_and__Bux.DTOs
{
    public class CreateProductReviewDto
    {
        public int ProductID { get; set; }
        public int OrderID { get; set; }
        public int Rating { get; set; }
        public string? ReviewTitle { get; set; }
        public string ReviewText { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
