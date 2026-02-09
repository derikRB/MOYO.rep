using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Interfaces
{
    public interface IProductTypeService
    {
        Task<ProductType> AddProductTypeAsync(ProductTypeDto dto);
        Task<ProductType?> GetProductTypeByIdAsync(int id);
        Task<IEnumerable<ProductType>> GetAllProductTypesAsync();
        Task<ProductType> UpdateProductTypeAsync(int id, ProductTypeDto dto);
        Task<bool> DeleteProductTypeAsync(int id);
    }
}
