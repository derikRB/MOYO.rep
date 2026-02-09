using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Sego_and__Bux.Hubs
{
    [Authorize(Roles = "Admin,Manager,Employee")]
    public class MetricsHub : Hub { }
}
