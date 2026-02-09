namespace Sego_and__Bux.Models
{
    public class UserActivityLog
    {
        public int AuditID { get; set; } // ✅ Primary key
        public int UserID { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string CriticalData { get; set; }
    }
}
