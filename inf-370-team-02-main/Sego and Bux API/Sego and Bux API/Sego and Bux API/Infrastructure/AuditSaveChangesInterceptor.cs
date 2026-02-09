using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sego_and__Bux.Data;
using Sego_and__Bux.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STJ = System.Text.Json.JsonSerializer;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<string> SensitiveProps = new()
        {
            "PasswordHash","OtpCode","OtpExpiry",
            "PasswordResetToken","PasswordResetTokenExpiry"
        };

        private static readonly HashSet<string> WatchedEntities = new()
        {
            // Your existing domain entities:
            "Order","OrderLine","Product","Customization","StockTransactionEntity",
            // NEW: account/profile entities
            "Customer","Employee"
        };

        private readonly IHttpContextAccessor _http;
        public AuditSaveChangesInterceptor(IHttpContextAccessor http) => _http = http;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is ApplicationDbContext ctx)
                WriteAuditRows(ctx, cancellationToken).GetAwaiter().GetResult();

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private async Task WriteAuditRows(ApplicationDbContext ctx, CancellationToken ct)
        {
            var now = System.DateTime.UtcNow;

            var entries = ctx.ChangeTracker.Entries()
                .Where(e => WatchedEntities.Contains(e.Metadata.ClrType.Name)
                         && (e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
                .ToList();

            if (entries.Count == 0) return;

            var audits = new List<AuditLog>(entries.Count);

            foreach (var e in entries)
            {
                var entityName = e.Metadata.ClrType.Name;

                // Avoid duplicate “register”: we log registration manually in controllers
                if ((entityName == "Customer" || entityName == "Employee") && e.State == EntityState.Added)
                    continue;

                var action = e.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => null
                };
                if (action == null) continue;

                string? beforeJson = null, afterJson = null, entityId = TryGetPrimaryKeyValue(e);
                string? critical = null;

                // Build sanitized snapshots
                if (e.State != EntityState.Added)
                    beforeJson = STJ.Serialize(ToDictSanitized(e.OriginalValues));
                if (e.State != EntityState.Deleted)
                    afterJson = STJ.Serialize(ToDictSanitized(e.CurrentValues));

                // Compact “critical” summary
                if (entityName == "Order")
                {
                    var total = e.Property("TotalPrice").CurrentValue;
                    var orderLines = e.Collection("OrderLines").CurrentValue as System.Collections.IEnumerable;
                    var qty = orderLines?.Cast<object>().Count() ?? 0;
                    critical = $"Total={total}; Items={qty}";
                }
                else if (entityName == "OrderLine")
                {
                    var pid = e.Property("ProductID").CurrentValue;
                    var qty = e.Property("Quantity").CurrentValue;
                    critical = $"ProductID={pid}; Qty={qty}";
                }
                else if (entityName == "StockTransactionEntity")
                {
                    var pid = e.Property("ProductID").CurrentValue;
                    var adj = e.Property("Adjusted").CurrentValue;
                    var rec = e.Property("Received").CurrentValue;
                    critical = $"ProductID={pid}; Adj={adj}; Rec={rec}";
                }
                else if (entityName is "Customer" or "Employee")
                {
                    var changed = ChangedFields(e);
                    var pwdChanged = changed.Contains("PasswordHash");
                    changed.Remove("PasswordHash"); // don’t list it
                    var fields = changed.Count == 0 ? "-" : string.Join(",", changed);
                    critical = $"Fields={fields}; PasswordChanged={(pwdChanged ? "true" : "false")}";
                }

                var (custId, empId, email, display) = await AuditHelpers.ResolveUserAsync(ctx, _http, ct);
                if (custId is null && empId is null) continue; // preserve CHECK constraint

                audits.Add(new AuditLog
                {
                    UtcTimestamp = now,
                    Action = action,
                    Entity = entityName,
                    EntityId = entityId,
                    BeforeJson = beforeJson,
                    AfterJson = afterJson,
                    CriticalValue = critical,
                    CustomerId = custId,
                    EmployeeId = empId,
                    UserEmailSnapshot = email,
                    UserDisplaySnapshot = display,
                    Ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = _http.HttpContext?.Request?.Headers.UserAgent.ToString()
                });
            }

            if (audits.Count > 0)
                await ctx.AuditLogs.AddRangeAsync(audits, ct);
        }

        private static Dictionary<string, object?> ToDictSanitized(PropertyValues values)
        {
            var dict = values.Properties.ToDictionary(p => p.Name, p => values[p.Name]);
            foreach (var k in SensitiveProps)
                if (dict.ContainsKey(k)) dict[k] = null; // redact
            return dict;
        }

        private static HashSet<string> ChangedFields(EntityEntry e)
        {
            var set = new HashSet<string>();
            foreach (var p in e.Properties)
            {
                if (!p.IsModified) continue;
                set.Add(p.Metadata.Name);
            }
            return set;
        }

        private static string? TryGetPrimaryKeyValue(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) return null;
            var parts = key.Properties
                .Select(p => entry.Property(p.Name).CurrentValue ?? entry.Property(p.Name).OriginalValue)
                .ToArray();
            return parts.Length == 1 ? parts[0]?.ToString() : STJ.Serialize(parts);
        }
    }
}
