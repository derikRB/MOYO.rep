public class Customer
{
    public int CustomerID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } = "Customer";

    // For OTP/Verification
    public bool IsVerified { get; set; } = false;
    public string? OtpCode { get; set; }                // Nullable
    public DateTime? OtpExpiry { get; set; }            // Nullable

    // For password reset feature:
    public string? PasswordResetToken { get; set; }     // Make this nullable!
    public DateTime? PasswordResetTokenExpiry { get; set; }  // Make this nullable!
}
