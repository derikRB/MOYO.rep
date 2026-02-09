using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Sego_and__Bux.DTOs
{
    public class CreateTemplateDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public IFormFile File { get; set; }

        [Required]
        public int ProductID { get; set; }    // ← associate to product
    }
}
