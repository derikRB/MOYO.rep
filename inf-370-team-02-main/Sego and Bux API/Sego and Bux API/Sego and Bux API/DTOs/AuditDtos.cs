// Sego_and__Bux/DTOs/AuditDtos.cs (or wherever your DTOs live)
using System;

namespace Sego_and__Bux.DTOs
{
    public class AuditQueryDto
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public string? User { get; set; }
        public string? Action { get; set; }
        public string? Entity { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class AuditLogDto
    {
        public long Id { get; set; }
        public DateTime UtcTimestamp { get; set; }

        // ✅ Canonical name used by API + frontend
        public string? UserEmail { get; set; }

        public string Action { get; set; } = default!;
        public string Entity { get; set; } = default!;
        public string? EntityId { get; set; }
        public string? CriticalValue { get; set; }
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
    }
}
