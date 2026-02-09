using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using System;
using System.Threading.Tasks;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class AuditWriter : IAuditWriter
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IHttpContextAccessor _http;

        public AuditWriter(ApplicationDbContext ctx, IHttpContextAccessor http)
        {
            _ctx = ctx;
            _http = http;
        }

        public async Task WriteAsync(
            string action, string entity, string? entityId,
            string? beforeJson, string? afterJson, string? criticalValue = null)
        {
            var (custId, empId, email, display) = await AuditHelpers.ResolveUserAsync(_ctx, _http);
            await WriteCoreAsync(custId, empId, email, display, action, entity, entityId, beforeJson, afterJson, criticalValue);
        }

        public async Task WriteForUserAsync(
            int? customerId, int? employeeId, string? email, string? display,
            string action, string entity, string? entityId,
            string? beforeJson, string? afterJson, string? criticalValue = null)
        {
            // If caller can’t resolve either side, fall back to normal resolution.
            if (customerId is null && employeeId is null)
            {
                var ctx = await AuditHelpers.ResolveUserAsync(_ctx, _http);
                await WriteCoreAsync(ctx.custId, ctx.empId, ctx.email, ctx.display, action, entity, entityId, beforeJson, afterJson, criticalValue);
            }
            else
            {
                await WriteCoreAsync(customerId, employeeId, email, display, action, entity, entityId, beforeJson, afterJson, criticalValue);
            }
        }

        private async Task WriteCoreAsync(
            int? custId, int? empId, string? email, string? display,
            string action, string entity, string? entityId,
            string? beforeJson, string? afterJson, string? criticalValue)
        {
            // Satisfy CHECK (must have either CustomerId or EmployeeId)
            if (custId is null && empId is null)
            {
                // soft-fallback to a system employee if present; otherwise skip
                var sys = await _ctx.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Email == "system@local");
                if (sys == null) return;
                empId = sys.EmployeeID;
                email ??= sys.Email;
                display ??= sys.Username ?? sys.Email;
            }

            var row = new AuditLog
            {
                UtcTimestamp = DateTime.UtcNow,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                BeforeJson = beforeJson,
                AfterJson = afterJson,
                CriticalValue = criticalValue,
                CustomerId = custId,
                EmployeeId = empId,
                UserEmailSnapshot = email,
                UserDisplaySnapshot = display,
                Ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = _http.HttpContext?.Request?.Headers.UserAgent.ToString()
            };

            _ctx.AuditLogs.Add(row);
            await _ctx.SaveChangesAsync();
        }
    }
}
