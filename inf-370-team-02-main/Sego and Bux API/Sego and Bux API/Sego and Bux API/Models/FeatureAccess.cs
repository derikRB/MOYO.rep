// Sego_and__Bux/Models/FeatureAccess.cs
using System;

namespace Sego_and__Bux.Models
{
    /// <summary>
    /// Serializable POCO for storing feature->roles mapping in JSON.
    /// </summary>
    public class FeatureAccess
    {
        public string FeatureKey { get; set; } = string.Empty;   // e.g., "reports"
        public string[] Roles { get; set; } = Array.Empty<string>(); // e.g., ["Admin","Manager","Employee"]
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = "system";
    }
}
