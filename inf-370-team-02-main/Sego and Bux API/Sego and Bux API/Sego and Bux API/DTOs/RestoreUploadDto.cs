using Microsoft.AspNetCore.Http;

namespace Sego_and__Bux.DTOs
{
    public class RestoreUploadDto
    {
        // Form field name: file
        public IFormFile File { get; set; } = default!;
    }
}
