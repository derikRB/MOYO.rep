using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 10;

        // FK → ProductType
        public int ProductTypeID { get; set; }
        public ProductType ProductType { get; set; } = null!;

        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<ProductTemplate> ProductTemplates { get; set; } = new List<ProductTemplate>();

        public int? PrimaryImageID { get; set; }
        public ProductImage? PrimaryImage { get; set; }

        public int? SecondaryImageID { get; set; }
        public ProductImage? SecondaryImage { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }
    }
}