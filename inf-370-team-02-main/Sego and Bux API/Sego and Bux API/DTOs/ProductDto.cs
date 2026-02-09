namespace Sego_and__Bux.DTOs
{
    public class ProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int ProductTypeID { get; set; }
        public ProductTypeDto? ProductType { get; set; }

        // ← allow the client to set or clear the primary image
        public int? PrimaryImageID { get; set; }
        public int? SecondaryImageID { get; set; } 
        public int LowStockThreshold { get; set; }    


    }
}
