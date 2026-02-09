using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface ITemplateService
    {
        Task<Template> CreateAsync(CreateTemplateDto dto);
        Task<IEnumerable<Template>> GetAllAsync();
        Task<Template?> GetByIdAsync(int id);
        Task<IEnumerable<Template>> GetByProductAsync(int productId);
        Task<Template?> UpdateAsync(int id, UpdateTemplateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
