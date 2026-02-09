using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sego_and__Bux.DTOs
{
    public class ProductResponseDto
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int ProductTypeID { get; set; }
        public int? PrimaryImageID { get; set; }
        public ProductImageDto? PrimaryImage { get; set; }
        public int? SecondaryImageID { get; set; }
        public ProductImageDto? SecondaryImage { get; set; }
        public List<ProductImageDto> ProductImages { get; set; }
            = new List<ProductImageDto>();
        // ...
        [JsonPropertyName("lowStockThreshold")]
        public int LowStockThreshold { get; set; }
        public ProductTypeDto ProductType { get; set; }
    }

}

