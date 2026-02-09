using Sego_and__Bux.Dto;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Services.Interfaces
{
    public interface IVatService
    {
        Task<List<Vat>> GetAllAsync();
        Task<Vat?> GetActiveAsync();
        Task<Vat> CreateAsync(VatDto dto);
        Task<Vat?> UpdateAsync(int id, VatDto dto);
        Task<Vat?> ActivateAsync(int id);
        Task<bool> ExistsByDateAsync(DateTime effectiveDate);
        Task<bool> ExistsByDateForOtherIdAsync(DateTime effectiveDate, int excludeId);
    }
}
