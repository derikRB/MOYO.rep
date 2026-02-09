using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Services
{
    public class TimerService : ITimerService
    {
        private readonly IAppConfigService _config;
        public TimerService(IAppConfigService config) => _config = config;

        public async Task<CurrentTimerStateDto> GetCurrentAsync(HttpContext httpContext)
        {
            var now = DateTime.UtcNow;

            // Pull policy (defaults if not set)
            var policy = await _config.GetTimerPolicyAsync();

            // Try read optional hints from cookies/headers (won’t fail if missing)
            httpContext.Request.Cookies.TryGetValue("otp_expires_at", out var otpExpCookie);
            httpContext.Request.Cookies.TryGetValue("sess_expires_at", out var sessExpCookie);

            // Fallbacks: if nothing is set by the client, return nulls (client UI should handle)
            string? otpExp = !string.IsNullOrWhiteSpace(otpExpCookie) ? otpExpCookie : null;
            string? sessExp = !string.IsNullOrWhiteSpace(sessExpCookie)
                ? sessExpCookie
                : now.AddMinutes(policy.SessionTimeoutMinutes).ToString("o");

            return new CurrentTimerStateDto
            {
                NowUtc = now.ToString("o"),
                OtpExpiresAtUtc = otpExp,
                SessionExpiresAtUtc = sessExp
            };
        }
    }
}