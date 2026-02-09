using Sego_and__Bux.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sego_and__Bux.Interfaces
{
    public interface IProductService
    {
        Task<ProductResponseDto> AddProductAsync(ProductDto dto);
        Task<ProductResponseDto?> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync();
        Task<ProductResponseDto?> UpdateProductAsync(int id, ProductDto dto);
        Task<bool> DeleteProductAsync(int id); // returns false when blocked
    }
}
