using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface ITimerService
    {
        Task<CurrentTimerStateDto> GetCurrentAsync(HttpContext httpContext);
    }
}