namespace Sego_and__Bux.DTOs
{
    public class ProductImageDto
    {
        public int ImageID { get; set; }
        // full URL path (client will prefix with /images/products/)
        public string ImageUrl { get; set; } = string.Empty;
    }
}
