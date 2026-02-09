using System.Threading.Tasks;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface IChatbotConfigService
    {
        Task<ChatbotConfigDto> GetAsync();
        Task<ChatbotConfigDto> UpdateAsync(ChatbotConfigDto dto);
    }
}
