using System;
using System.ComponentModel.DataAnnotations;

namespace Sego_and__Bux.Models
{
    public class Feedback
    {
        public int FeedbackID { get; set; }
        public int UserID { get; set; }         // CustomerID (FK)
        public int OrderID { get; set; }        // OrderID (FK)
        public int Rating { get; set; }         // 1-5 stars
        [Required]
        public string Comments { get; set; } = "";
        public bool Recommend { get; set; }
        public DateTime SubmittedDate { get; set; }
    }
}
