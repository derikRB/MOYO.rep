using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;
using System.Threading.Tasks;

namespace Sego_and__Bux.Interfaces
{
    public interface ICustomerService
    {
        Task<bool> ExistsByUsernameOrEmailAsync(string username, string email);
        Task<Customer?> GetCustomerByUsernameOrEmailAsync(string emailOrUsername);
        Task<Customer> RegisterAsync(RegisterCustomerDto registerDto);
        Task<CustomerDto?> GetCustomerByIdAsync(int id);
        Task<Customer?> UpdateCustomerAsync(int id, UpdateCustomerDto dto);
        Task<bool> DeleteCustomerAsync(int id);
        Task SaveChangesAsync();
        Task<bool> UpdatePasswordAsync(int id, string currentPassword, string newPassword);
    }
}
