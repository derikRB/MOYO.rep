// EmployeeService.cs
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.Helpers;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Sego_and__Bux.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sego_and__Bux.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        public EmployeeService(ApplicationDbContext context) => _context = context;

        public async Task<Employee> RegisterEmployeeAsync(LoginDto dto)
        {
            if (await _context.Employees
                   .AnyAsync(e => e.Username == dto.EmailOrUsername || e.Email == dto.EmailOrUsername))
                throw new Exception("Username or email already exists.");

            var emp = new Employee
            {
                Username = dto.EmailOrUsername,
                Email = dto.EmailOrUsername,
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                Role = "Employee"
            };

            _context.Employees.Add(emp);
            await _context.SaveChangesAsync();
            return emp;
        }

        public async Task<Employee?> GetByUsernameOrEmailAsync(string emailOrUsername)
        {
            return await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Username == emailOrUsername
                                       || e.Email == emailOrUsername);
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
            => await _context.Employees.AsNoTracking().ToListAsync();

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string term)
            => await _context.Customers
                .Where(c => c.Name.Contains(term)
                         || c.Surname.Contains(term)
                         || c.Email.Contains(term))
                .AsNoTracking()
                .ToListAsync();

        public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string term)
            => await _context.Employees
                .Where(e => e.Username.Contains(term)
                         || (e.Email != null && e.Email.Contains(term))
                         || (e.Role != null && e.Role.Contains(term)))
                .AsNoTracking()
                .ToListAsync();
    }
}
