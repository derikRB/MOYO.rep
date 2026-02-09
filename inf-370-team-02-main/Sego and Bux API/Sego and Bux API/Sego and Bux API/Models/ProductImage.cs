using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }
        public int ProductID { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // nav back to product
        public Product Product { get; set; } = null!;

        // computed convenience property:
        [NotMapped]
        public string Url => $"/images/products/{ImagePath}";
    }
}
