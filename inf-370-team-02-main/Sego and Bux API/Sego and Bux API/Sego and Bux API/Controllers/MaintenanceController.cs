using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sego_and__Bux.Config;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Infrastructure;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/maintenance")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _svc;
        private readonly IAuditWriter _audit;
        private readonly MaintenanceState _state;

        public MaintenanceController(
            IMaintenanceService svc,
            IAuditWriter audit,
            MaintenanceState state)
        {
            _svc = svc;
            _audit = audit;
            _state = state;
        }

        [HttpPost("backup")]
        public async Task<IActionResult> Backup()
        {
            var (fileName, contentType, stream) = await _svc.CreateBackupAsync();

            // audit
            await _audit.WriteAsync(
                "Create", "Maintenance", "Backup",
                null, fileName, null);

            // File() sets Content-Disposition with the filename => browsers save as .bak correctly
            return File(stream, contentType, fileName);
        }

        [HttpPost("restore")]
        [RequestSizeLimit(1024L * 1024L * 1024L)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Restore([FromForm] RestoreUploadDto form)
        {
            if (!_state.AllowRestore)
                return Problem(statusCode: 403, title: "Restore disabled",
                    detail: "Database restore is disabled by configuration for this environment.");

            var file = form.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            // audit
            await _audit.WriteAsync(
                "Update", "Maintenance", "Restore-Requested",
                null, null, file.FileName);

            // turn on maintenance so non-admin users get 503 during restore
            _state.Enabled = true;
            _state.Message ??= "Applying database restore…";

            await _svc.ScheduleRestoreAsync(file);
            return Accepted(new { message = "Restore scheduled. The application may restart to complete the operation." });
        }

        [HttpPost("mode")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> SetMode([FromBody] MaintenanceModeDto dto)
        {
            _state.Enabled = dto.Enabled;
            _state.Message = string.IsNullOrWhiteSpace(dto.Message) ? _state.Message : dto.Message;

            await _audit.WriteAsync(
                "Update", "Maintenance", "Mode",
                null, _state.Enabled ? "Enabled" : "Disabled", _state.Message);

            return Ok(new { _state.Enabled, _state.Message });
        }

        [HttpGet("mode")]
        [AllowAnonymous]
        public IActionResult GetMode() => Ok(new { _state.Enabled, _state.Message, _state.AllowRestore });
    }
}