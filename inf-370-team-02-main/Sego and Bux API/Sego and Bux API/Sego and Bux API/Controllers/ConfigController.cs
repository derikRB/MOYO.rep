using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/config")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public class ConfigController : ControllerBase
    {
        private readonly IAppConfigService _config;
        private readonly IAuditWriter _audit;

        public ConfigController(IAppConfigService config, IAuditWriter audit)
        {
            _config = config;
            _audit = audit;
        }

        [HttpGet("timers")]
        public async Task<ActionResult<TimerPolicyDto>> GetTimers()
        {
            var policy = await _config.GetTimerPolicyAsync();
            return Ok(policy);
        }

        [HttpGet("otp-minutes")]
        [AllowAnonymous]
        public async Task<ActionResult<int>> GetOtpMinutes()
        {
            var policy = await _config.GetTimerPolicyAsync();
            return Ok(policy.OtpExpiryMinutes);
        }

        [HttpPut("timers")]
        public async Task<IActionResult> UpdateTimers([FromBody] TimerPolicyDto dto)
        {
            var userEmail = User?.Identity?.Name ?? "system";
            await _config.UpdateTimerPolicyAsync(dto.OtpExpiryMinutes, dto.SessionTimeoutMinutes, userEmail);

            // action, entity, entityId, beforeJson, afterJson, criticalValue
            await _audit.WriteAsync(
                "Update",
                "SystemConfig",
                "Timers",
                null,
                null,
                $"Otp={dto.OtpExpiryMinutes},Session={dto.SessionTimeoutMinutes}"
            );

            return NoContent();
        }
    }
}
