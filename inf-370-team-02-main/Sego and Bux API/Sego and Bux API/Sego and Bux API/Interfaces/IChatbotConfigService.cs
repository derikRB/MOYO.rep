using System.Threading.Tasks;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface IChatbotConfigService
    {
        // Nullable so callers can defensively handle an empty table.
        Task<ChatbotConfigDto?> GetAsync();
        Task<ChatbotConfigDto?> UpdateAsync(ChatbotConfigDto dto);
    }
}
