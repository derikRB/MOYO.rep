using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Helpers;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;

namespace Sego_and__Bux.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        public AuditService(ApplicationDbContext context) => _context = context;

        public async Task LogActivityAsync(int userId, string role, string controller, string action, string criticalData)
        {
            var log = new UserActivityLog
            {
                UserID = userId,
                Controller = controller,
                Action = role + ": " + action,
                CriticalData = criticalData
            };
            _context.UserActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserActivityLog>> GetAllLogsAsync() => await _context.UserActivityLogs.OrderByDescending(l => l.Timestamp).ToListAsync();

        public async Task<IEnumerable<UserActivityLog>> GetLogsByUserIdAsync(int userId) =>
            await _context.UserActivityLogs.Where(l => l.UserID == userId).OrderByDescending(l => l.Timestamp).ToListAsync();
    }

}
