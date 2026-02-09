using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.Interfaces;

namespace SegoAndBux.Tests.Fakes
{
    public class FakeAuditWriter : IAuditWriter
    {
        public record AuditCall(string Action, string Entity, string EntityId, string? Before, string? After, string? Critical);
        public List<AuditCall> Calls { get; } = new();

        public Task WriteAsync(string action, string entity, string entityId, string? beforeJson, string? afterJson, string? criticalValue)
        {
            Calls.Add(new AuditCall(action, entity, entityId, beforeJson, afterJson, criticalValue));
            return Task.CompletedTask;
        }
    }
}
