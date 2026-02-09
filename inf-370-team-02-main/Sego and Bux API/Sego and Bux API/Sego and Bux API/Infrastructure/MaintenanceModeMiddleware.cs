using System.Security.Claims;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MaintenanceState _state;

        public MaintenanceModeMiddleware(RequestDelegate next, MaintenanceState state)
        {
            _next = next;
            _state = state;
        }

        public async Task Invoke(HttpContext ctx)
        {
            if (_state.Enabled)
            {
                // Allow health checks, swagger, hubs, static files
                var path = ctx.Request.Path.Value ?? string.Empty;
                if (!path.StartsWith("/swagger") &&
                    !path.StartsWith("/hubs") &&
                    !path.StartsWith("/waybills") &&
                    !path.StartsWith("/customizations") &&
                    !path.StartsWith("/images") &&
                    !path.StartsWith("/templates") &&
                    !path.StartsWith("/reviews"))
                {
                    var isBypass = _state.BypassRoles.Any(r => ctx.User.IsInRole(r));
                    if (!isBypass)
                    {
                        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await ctx.Response.WriteAsync(_state.Message ?? "Down for maintenance");
                        return;
                    }
                }
            }

            await _next(ctx);
        }
    }
}
