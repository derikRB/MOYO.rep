using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Data;
using Sego_and__Bux.Models;
using System.Threading.Tasks;
using Sego_and__Bux.Audit;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ApplicationDbContext _context;
        private readonly IAuditWriter _audit;

        public EmployeeController(IEmployeeService employeeService, ApplicationDbContext context, IAuditWriter audit)
        {
            _employeeService = employeeService;
            _context = context;
            _audit = audit;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(LoginDto dto)
        {
            var employee = await _employeeService.RegisterEmployeeAsync(dto);

            await _audit.WriteForUserAsync(
                null, employee.EmployeeID, employee.Email, employee.Username,
                AuditEvent.Employee.Register, "Employee", employee.EmployeeID.ToString(),
                null, null, $"Role={employee.Role}");

            return Ok(employee);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            return Ok(employees);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id, true);
            if (employee == null)
                return NotFound();

            // Use soft delete instead of hard delete
            var success = await _employeeService.SoftDeleteEmployeeAsync(id);

            if (!success)
                return BadRequest("Unable to delete employee.");

            await _audit.WriteForUserAsync(
                null, id, employee.Email, employee.Username,
                AuditEvent.Employee.Delete, "Employee", id.ToString(),
                null, null, "Result=Success (Soft Delete)");

            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeeDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null || !employee.IsActive)
                return NotFound();

            var before = new { employee.Username, employee.Email, employee.Role };

            employee.Username = dto.Username;
            employee.Email = dto.Email;
            employee.Role = dto.Role;

            await _context.SaveChangesAsync();

            await _audit.WriteForUserAsync(
                null, employee.EmployeeID, employee.Email, employee.Username,
                AuditEvent.Employee.Update, "Employee", id.ToString(),
                System.Text.Json.JsonSerializer.Serialize(before),
                System.Text.Json.JsonSerializer.Serialize(dto),
                "Fields=Username,Email,Role");

            return Ok(employee);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var results = await _employeeService.SearchEmployeesAsync(q);
            return Ok(results);
        }

        // NEW: Endpoint to get employee by ID (optional include inactive)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id, [FromQuery] bool includeInactive = false)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id, includeInactive);
            if (employee == null)
                return NotFound();

            return Ok(employee);
        }
    }
}