namespace Sego_and__Bux.DTOs
{
    public class TimerPolicyDto
    {
        public int OtpExpiryMinutes { get; set; }
        public int SessionTimeoutMinutes { get; set; }
        public int MinOtpMinutes { get; set; } = 1;
        public int MaxOtpMinutes { get; set; } = 30;
        public int MinSessionMinutes { get; set; } = 5;
        public int MaxSessionMinutes { get; set; } = 240;
        public string UpdatedAtUtc { get; set; } = "";
    }
}

   
