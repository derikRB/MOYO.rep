// Controllers/TimersController.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/timers")]
    [Authorize] // any signed-in user can see the current values
    public class TimersController : ControllerBase
    {
        private readonly ITimerService _svc;
        public TimersController(ITimerService svc) => _svc = svc;

        [HttpGet("current")]
        public async Task<ActionResult<CurrentTimerStateDto>> GetCurrent()
        {
            var state = await _svc.GetCurrentAsync(HttpContext);
            return Ok(state);
        }
    }
}