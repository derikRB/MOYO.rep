namespace Sego_and__Bux.DTOs
{
    public class ProductReviewDto
    {
        public int ReviewID { get; set; }
        public int ProductID { get; set; }
        public int UserID { get; set; }
        public int OrderID { get; set; }
        public int Rating { get; set; }
        public string? ReviewTitle { get; set; }
        public string ReviewText { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; }
        public string UserName { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
    }
}
