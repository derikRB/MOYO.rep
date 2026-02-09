using System;

namespace Sego_and__Bux.DTOs
{
    public class CurrentTimerStateDto
    {
        public string NowUtc { get; set; } = DateTime.UtcNow.ToString("O");
        public string? OtpExpiresAtUtc { get; set; }
        public string? SessionExpiresAtUtc { get; set; }
    }
}