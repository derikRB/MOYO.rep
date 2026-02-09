using System.Threading.Tasks;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface IAppConfigService
    {
        Task<int> GetIntAsync(string key, int @default);
        Task<TimerPolicyDto> GetTimerPolicyAsync(CancellationToken ct = default);
        Task UpdateTimerPolicyAsync(int otpMinutes, int sessionMinutes, string changedBy, CancellationToken ct = default);
    }

}
