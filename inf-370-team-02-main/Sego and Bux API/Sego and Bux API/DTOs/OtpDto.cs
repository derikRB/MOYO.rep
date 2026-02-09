namespace Sego_and__Bux.DTOs
{
    public class OtpVerifyDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class OtpResendDto
    {
        public string Email { get; set; }
    }
}
