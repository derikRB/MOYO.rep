using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Sego_and__Bux.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetCustomerByUsernameOrEmailAsync(string emailOrUsername)
        {
            var normalizedPhone = NormalizeZaPhone(emailOrUsername);

            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return await _context.Customers.FirstOrDefaultAsync(c =>
                    (c.Phone == normalizedPhone || c.Email == emailOrUsername || c.Username == emailOrUsername)
                    && !c.IsDeleted
                );
            }

            return await _context.Customers.FirstOrDefaultAsync(c =>
                (c.Email == emailOrUsername || c.Username == emailOrUsername)
                && !c.IsDeleted
            );
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerID == id && !c.IsDeleted)
                .Select(c => new CustomerDto
                {
                    Id = c.CustomerID,
                    Username = c.Username,
                    Name = c.Name,
                    Surname = c.Surname,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address
                })
                .FirstOrDefaultAsync();

            return customer;
        }

        public async Task<Customer?> UpdateCustomerAsync(int id, UpdateCustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || customer.IsDeleted) return null;

            customer.Username = dto.Username;
            customer.Name = dto.Name;
            customer.Surname = dto.Surname;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;

            await _context.SaveChangesAsync();
            return customer;
        }

        // ✅ SOFT DELETE IMPLEMENTATION (Preserves orders)
        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || customer.IsDeleted) return false;

            // Soft delete - anonymize personal data but keep orders intact
            customer.IsDeleted = true;
            customer.DeletedAt = DateTime.UtcNow;

            // Anonymize personal information but keep the record for order integrity
            customer.Username = $"deleted_user_{customer.CustomerID}_{DateTime.UtcNow.Ticks}";
            customer.Name = "Deleted";
            customer.Surname = "User";
            customer.Email = $"deleted_{customer.CustomerID}@deleted.example";
            customer.Phone = "0000000000";
            customer.Address = "Address Removed";
            customer.PasswordHash = "DELETED";

            // Clear sensitive verification data
            customer.OtpCode = null;
            customer.OtpExpiry = null;
            customer.PasswordResetToken = null;
            customer.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdatePasswordAsync(int id, string currentPassword, string newPassword)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || customer.IsDeleted) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, customer.PasswordHash))
                return false;

            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Customer> RegisterAsync(RegisterCustomerDto registerDto)
        {
            var existingCustomer = await _context.Customers
                .AnyAsync(c => (c.Email == registerDto.Email || c.Username == registerDto.Username) && !c.IsDeleted);
            if (existingCustomer)
                throw new System.Exception("Username or Email already exists.");

            var customer = new Customer
            {
                Username = registerDto.Username,
                Name = registerDto.Name,
                Surname = registerDto.Surname,
                Email = registerDto.Email,
                Phone = registerDto.Phone,
                Address = registerDto.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email)
        {
            return await _context.Customers.AnyAsync(c =>
                (c.Username == username || c.Email == email) && !c.IsDeleted);
        }

        private static string? NormalizeZaPhone(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var digits = new string(input.Where(char.IsDigit).ToArray());

            if (digits.Length == 10 && digits[0] == '0')
                return digits;

            if (digits.Length == 11 && digits.StartsWith("27"))
                return "0" + digits.Substring(2);

            return null;
        }
    }
}