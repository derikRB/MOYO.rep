using Sego_and__Bux.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sego_and__Bux.Interfaces
{
    public interface IFaqService
    {
        Task<List<FaqItemDto>> GetAllAsync();
        Task<FaqItemDto> GetByIdAsync(int id);
        Task<FaqItemDto> CreateAsync(FaqItemDto dto);
        Task<FaqItemDto> UpdateAsync(FaqItemDto dto);
        Task DeleteAsync(int id);
        Task<List<FaqItemDto>> SearchAsync(string q);
    }
}
