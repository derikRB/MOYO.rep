using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;

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
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == emailOrUsername || c.Username == emailOrUsername);
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerID == id) // Use CustomerID here
                .Select(c => new CustomerDto
                {
                    Id = c.CustomerID, // Map to DTO Id
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
            if (customer == null) return null;

            customer.Username = dto.Username;
            customer.Name = dto.Name;
            customer.Surname = dto.Surname;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;

            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return false;

            _context.Customers.Remove(customer);
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
            if (customer == null) return false;

            // ✅ Check current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, customer.PasswordHash))
                return false;

            // ✅ Hash new password and update
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<Customer> RegisterAsync(RegisterCustomerDto registerDto)
        {
            var existingCustomer = await _context.Customers
                .AnyAsync(c => c.Email == registerDto.Email || c.Username == registerDto.Username);
            if (existingCustomer)
            {
                throw new System.Exception("Username or Email already exists.");
            }

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
                c.Username == username || c.Email == email);
        }

    }
}
