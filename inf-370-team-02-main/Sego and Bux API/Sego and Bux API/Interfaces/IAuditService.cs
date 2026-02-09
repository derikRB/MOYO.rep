
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface IAuditService
    {
        Task LogActivityAsync(int userId, string role, string controller, string action, string criticalData);
        Task<IEnumerable<UserActivityLog>> GetAllLogsAsync();
        Task<IEnumerable<UserActivityLog>> GetLogsByUserIdAsync(int userId);
    }
}
