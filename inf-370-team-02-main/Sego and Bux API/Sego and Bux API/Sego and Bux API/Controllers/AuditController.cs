using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AuditController(ApplicationDbContext db) => _db = db;

        public sealed class AuditQueryDto
        {
            public System.DateTime? FromUtc { get; set; }
            public System.DateTime? ToUtc { get; set; }
            public string? User { get; set; }
            public string? Action { get; set; }
            public string? Entity { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 25;
        }

        [HttpGet]
        public async Task<IActionResult> Query([FromQuery] AuditQueryDto q)
        {
            if (q.Page <= 0) q.Page = 1;
            if (q.PageSize <= 0 || q.PageSize > 500) q.PageSize = 25;

            var query = _db.AuditLogs.AsNoTracking();

            if (q.FromUtc.HasValue) query = query.Where(x => x.UtcTimestamp >= q.FromUtc.Value);
            if (q.ToUtc.HasValue) query = query.Where(x => x.UtcTimestamp <= q.ToUtc.Value);
            if (!string.IsNullOrWhiteSpace(q.User))
            {
                var u = q.User.Trim();
                query = query.Where(x =>
                    (x.UserEmailSnapshot ?? "").Contains(u) ||
                    (x.UserDisplaySnapshot ?? "").Contains(u));
            }
            if (!string.IsNullOrWhiteSpace(q.Action)) query = query.Where(x => x.Action == q.Action);
            if (!string.IsNullOrWhiteSpace(q.Entity)) query = query.Where(x => x.Entity == q.Entity);

            var total = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(x => x.UtcTimestamp)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(x => new
                {
                    id = x.Id,
                    utcTimestamp = x.UtcTimestamp,
                    userEmail = x.UserEmailSnapshot,
                    userDisplay = x.UserDisplaySnapshot,
                    action = x.Action,
                    entity = x.Entity,
                    entityId = x.EntityId,
                    criticalValue = x.CriticalValue
                })
                .ToListAsync();

            return Ok(new { page = q.Page, pageSize = q.PageSize, total, items });
        }
    }
}