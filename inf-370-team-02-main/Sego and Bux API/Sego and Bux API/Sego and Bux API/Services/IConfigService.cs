using System.Threading.Tasks;

namespace Sego_and__Bux.Services.Interfaces
{
    public interface IConfigService
    {
        Task<int> GetIntAsync(string key, int @default);
        Task SetIntAsync(string key, int value, string updatedBy);

        Task<(int otp, int session, string updatedAtUtc)> GetTimerPolicyAsync();
        Task UpdateTimerPolicyAsync(int otpMinutes, int sessionMinutes, string updatedBy);
    }
}
