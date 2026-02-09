using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    public class ProductType
    {
        [Key]
        public int ProductTypeID { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductTypeName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int CategoryID { get; set; }

        public Category? Category { get; set; }
    }
}
