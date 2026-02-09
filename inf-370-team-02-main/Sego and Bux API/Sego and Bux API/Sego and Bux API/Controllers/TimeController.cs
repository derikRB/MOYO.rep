using System;
using Microsoft.AspNetCore.Mvc;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/time")]
    public sealed class TimeController : ControllerBase
    {
        // South Africa does not observe DST; offset is always +02:00.
        [HttpGet("now")]
        public IActionResult Now() => Ok(new
        {
            nowUtc = DateTime.UtcNow,
            timezone = "Africa/Johannesburg",
            offsetMinutes = 120
        });
    }
}
