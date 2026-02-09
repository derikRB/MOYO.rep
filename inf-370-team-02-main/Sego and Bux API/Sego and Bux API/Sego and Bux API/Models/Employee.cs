namespace Sego_and__Bux.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public bool IsActive { get; set; } = true; // ← ADD THIS
    }
}