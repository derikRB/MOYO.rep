using System;
using System.ComponentModel.DataAnnotations;

namespace Sego_and__Bux.DTOs
{
    public class FeedbackDto
    {
        public int FeedbackID { get; set; }
        public int UserID { get; set; }
        public int OrderID { get; set; }
        public int Rating { get; set; }
        public string Comments { get; set; } = string.Empty;
        public bool Recommend { get; set; }
        public DateTime SubmittedDate { get; set; }

        // NEW: lets UI show "Name (#ID)"
        public string? UserName { get; set; }
    }

    public class CreateFeedbackDto
    {
        [Required]
        public int OrderID { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        public string Comments { get; set; } = string.Empty;

        public bool Recommend { get; set; }
    }
}
