using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Sego_and__Bux.DTOs
{
    public class UpdateTemplateDto
    {
        [Required]
        public string Name { get; set; }

        public IFormFile? File { get; set; }

        [Required]
        public int ProductID { get; set; }    // ← allow re‐linking
    }
}
