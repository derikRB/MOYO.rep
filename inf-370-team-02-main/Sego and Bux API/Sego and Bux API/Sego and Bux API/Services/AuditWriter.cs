using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Services
{
    /// <summary>
    /// Unified audit writer. Matches the 6-arg signature used by your controllers.
    /// Auto-fills CustomerId/EmployeeId, email, display, IP and user agent so
    /// CK_AuditLogs_UserChoice is satisfied.
    /// </summary>
    public class AuditWriter : IAuditWriter
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        public AuditWriter(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task WriteAsync(
            string action,
            string entity,
            string? entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue)
        {
            var (custId, empId, email, display) = await ResolveUserAsync();
            await WriteRowAsync(custId, empId, email, display, action, entity, entityId, beforeJson, afterJson, criticalValue);
        }

        /// <summary>
        /// Explicit-identity write (for pre-auth flows like register/login/verify).
        /// If both IDs are null, it falls back to ResolveUserAsync() and then to a system account if needed.
        /// </summary>
        public async Task WriteForUserAsync(
            int? customerId,
            int? employeeId,
            string? email,
            string? display,
            string action,
            string entity,
            string? entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue)
        {
            if (customerId is null && employeeId is null)
            {
                var resolved = await ResolveUserAsync();
                customerId ??= resolved.custId;
                employeeId ??= resolved.empId;
                email ??= resolved.email;
                display ??= resolved.display;
            }

            await WriteRowAsync(customerId, employeeId, email, display, action, entity, entityId, beforeJson, afterJson, criticalValue);
        }

        private async Task WriteRowAsync(
            int? custId,
            int? empId,
            string? email,
            string? display,
            string action,
            string entity,
            string? entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue)
        {
            // Fallback to a system employee if neither side is resolvable, to avoid check-constraint failures.
            if (custId is null && empId is null)
            {
                var systemEmp = await _db.Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Email == "system@local");
                if (systemEmp != null)
                {
                    empId = systemEmp.EmployeeID;
                    email ??= systemEmp.Email;
                    display ??= systemEmp.Username ?? systemEmp.Email;
                }
                else
                {
                    // Skip writing rather than violating CK_AuditLogs_UserChoice
                    return;
                }
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

            _db.AuditLogs.Add(row);
            await _db.SaveChangesAsync();
        }

        private async Task<(int? custId, int? empId, string? email, string? display)> ResolveUserAsync()
        {
            var http = _http.HttpContext;
            int? customerId = null, employeeId = null;

            string? email =
                http?.User?.FindFirstValue(ClaimTypes.Email) ??
                http?.User?.Identity?.Name;

            string? display = null;

            // Try numeric nameidentifier first (ID-based resolution)
            var idClaim = http?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? http?.User?.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;

            if (int.TryParse(idClaim, out var idFromToken))
            {
                var cust = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerID == idFromToken);
                if (cust != null)
                {
                    customerId = cust.CustomerID;
                    email ??= cust.Email;
                    display = !string.IsNullOrWhiteSpace(cust.Username)
                        ? cust.Username
                        : $"{(cust.Name ?? "").Trim()} {(cust.Surname ?? "").Trim()}".Trim();
                }
                else
                {
                    var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == idFromToken);
                    if (emp != null)
                    {
                        employeeId = emp.EmployeeID;
                        email ??= emp.Email;
                        display = string.IsNullOrWhiteSpace(emp.Username) ? (emp.Email ?? "employee") : emp.Username;
                    }
                }
            }

            // If still unresolved, try email lookup
            if (employeeId is null && customerId is null && !string.IsNullOrWhiteSpace(email))
            {
                var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Email == email);
                if (emp != null)
                {
                    employeeId = emp.EmployeeID;
                    display ??= string.IsNullOrWhiteSpace(emp.Username) ? (emp.Email ?? "employee") : emp.Username;
                }
                else
                {
                    var cust = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email);
                    if (cust != null)
                    {
                        customerId = cust.CustomerID;
                        display ??= !string.IsNullOrWhiteSpace(cust.Username)
                            ? cust.Username
                            : $"{(cust.Name ?? "").Trim()} {(cust.Surname ?? "").Trim()}".Trim();
                        if (string.IsNullOrWhiteSpace(display)) display = cust.Email;
                    }
                }
            }

            return (customerId, employeeId, email, display);
        }
    }
}
