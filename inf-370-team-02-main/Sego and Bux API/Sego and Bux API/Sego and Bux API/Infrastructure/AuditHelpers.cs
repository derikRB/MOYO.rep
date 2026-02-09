using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sego_and__Bux.Infrastructure
{
    /// <summary>Shared helper used by both the interceptor and the manual writer.</summary>
    internal static class AuditHelpers
    {
        /// <summary>
        /// Resolve user context to satisfy CK_AuditLogs_UserChoice.
        /// Returns (customerId, employeeId, email, display).
        /// </summary>
        internal static async Task<(int? custId, int? empId, string? email, string? display)>
            ResolveUserAsync(ApplicationDbContext ctx, IHttpContextAccessor httpAcc, CancellationToken ct = default)
        {
            var http = httpAcc.HttpContext;
            int? customerId = null, employeeId = null;
            string? email = http?.User?.FindFirstValue(ClaimTypes.Email) ?? http?.User?.Identity?.Name;
            string? display = null;

            var idClaim = http?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? http?.User?.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;

            if (int.TryParse(idClaim, out var id))
            {
                var cust = await ctx.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerID == id, ct);
                if (cust != null)
                {
                    customerId = cust.CustomerID;
                    email ??= cust.Email;
                    display = !string.IsNullOrWhiteSpace(cust.Username)
                        ? cust.Username
                        : $"{(cust.Name ?? "").Trim()} {(cust.Surname ?? "").Trim()}".Trim();
                    if (string.IsNullOrWhiteSpace(display)) display = email ?? "customer";
                }
                else
                {
                    var emp = await ctx.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == id, ct);
                    if (emp != null)
                    {
                        employeeId = emp.EmployeeID;
                        email ??= emp.Email;
                        display = string.IsNullOrWhiteSpace(emp.Username) ? (emp.Email ?? "employee") : emp.Username;
                    }
                }
            }

            if (employeeId is null && customerId is null && !string.IsNullOrWhiteSpace(email))
            {
                var emp = await ctx.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Email == email, ct);
                if (emp != null)
                {
                    employeeId = emp.EmployeeID;
                    display ??= string.IsNullOrWhiteSpace(emp.Username) ? (emp.Email ?? "employee") : emp.Username;
                }
                else
                {
                    var cust = await ctx.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email, ct);
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

            // Final fallback (to avoid breaking the CHECK constraint)
            if (employeeId is null && customerId is null)
            {
                var sysEmp = await ctx.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Email == "system@local", ct);
                if (sysEmp != null)
                {
                    employeeId = sysEmp.EmployeeID;
                    email ??= sysEmp.Email;
                    display ??= sysEmp.Username ?? sysEmp.Email;
                }
            }

            return (customerId, employeeId, email, display);
        }
    }
}