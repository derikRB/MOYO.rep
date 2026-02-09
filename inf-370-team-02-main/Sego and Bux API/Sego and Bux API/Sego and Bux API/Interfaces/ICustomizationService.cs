
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface ICustomizationService
    {
        Task<Customization> AddCustomizationAsync(CustomizationDto dto);
        Task<Customization?> GetByOrderLineIdAsync(int orderLineId);
        Task<Customization> UpdateCustomizationAsync(int orderLineId, CustomizationDto dto);
        Task<bool> DeleteCustomizationAsync(int orderLineId);
    }
}
