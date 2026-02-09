using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sego_and__Bux.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int UserID { get; set; }
        [ForeignKey("UserID")]
        public Customer Customer { get; set; }

        [Required]
        public int ProductID { get; set; }
        [ForeignKey("ProductID")]
        public Product Product { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(200)]
        public string? ReviewTitle { get; set; }

        [Required]
        public string ReviewText { get; set; }

        [MaxLength(200)]
        public string? PhotoFileName { get; set; }

        [Required]
        public DateTime SubmittedDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Declined
    }
}
