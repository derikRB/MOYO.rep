using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sego_and__Bux.Interfaces
{
    public interface IEmployeeService
    {
        Task<Employee> RegisterEmployeeAsync(LoginDto dto);
        Task<Employee?> GetByUsernameOrEmailAsync(string emailOrUsername);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<IEnumerable<Customer>> SearchCustomersAsync(string term);
        Task<IEnumerable<Employee>> SearchEmployeesAsync(string term);

        // NEW: Add these methods
        Task<bool> SoftDeleteEmployeeAsync(int employeeId);
        Task<Employee?> GetEmployeeByIdAsync(int id, bool includeInactive = false);
    }
}