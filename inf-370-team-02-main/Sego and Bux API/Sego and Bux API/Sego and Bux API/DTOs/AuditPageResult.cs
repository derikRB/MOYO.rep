using System;
using System.Collections.Generic;

namespace Sego_and__Bux.DTOs
{
    public class AuditPageResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long Total { get; set; }
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    }
}
