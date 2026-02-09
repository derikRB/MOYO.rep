using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.Interfaces;

namespace SegoAndBux.Tests.Fakes
{
    public class FakeAuditWriter : IAuditWriter
    {
        public record AuditCall(
            int? UserId,
            int? EmployeeId,
            string? UserEmail,
            string? UserDisplay,
            string Action,
            string Entity,
            string EntityId,
            string? BeforeJson,
            string? AfterJson,
            string? CriticalValue
        );

        public List<AuditCall> Calls { get; } = new();

        // Existing interface member
        public Task WriteAsync(
            string action,
            string entity,
            string entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue)
        {
            Calls.Add(new AuditCall(
                null, null, null, null,
                action, entity, entityId, beforeJson, afterJson, criticalValue));
            return Task.CompletedTask;
        }

        // New interface member
        public Task WriteForUserAsync(
            int? userId,
            int? employeeId,
            string? userEmail,
            string? userDisplay,
            string action,
            string entity,
            string entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue)
        {
            Calls.Add(new AuditCall(
                userId, employeeId, userEmail, userDisplay,
                action, entity, entityId, beforeJson, afterJson, criticalValue));
            return Task.CompletedTask;
        }
    }
}
