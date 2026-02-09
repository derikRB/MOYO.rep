using System;
using System.Collections.Generic;


namespace Sego_and__Bux.Models
{
    public class SystemConfig
    {
        public int Id { get; set; }
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}