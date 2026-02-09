using System.Threading.Tasks;

namespace Sego_and__Bux.Interfaces
{
    public interface IAuditWriter
    {
        /// <summary>
        /// Resolve actor from the current request (Customer/Employee from token/claims) and write an audit row.
        /// </summary>
        Task WriteAsync(
            string action,
            string entity,
            string? entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue = null);

        /// <summary>
        /// Explicit-identity write (for pre-auth flows like register/login/verify).
        /// Pass either customerId or employeeId (or both null to fallback to resolver).
        /// </summary>
        Task WriteForUserAsync(
            int? customerId,
            int? employeeId,
            string? email,
            string? display,
            string action,
            string entity,
            string? entityId,
            string? beforeJson,
            string? afterJson,
            string? criticalValue = null);
    }
}
