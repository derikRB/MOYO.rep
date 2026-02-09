using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using System.Threading.Tasks;
using Sego_and__Bux.Audit;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IAuditWriter _audit;

        public CustomerController(ICustomerService customerService, IAuditWriter audit)
        {
            _customerService = customerService;
            _audit = audit;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            var dto = new CustomerDto
            {
                Id = customer.Id,
                Username = customer.Username,
                Name = customer.Name,
                Surname = customer.Surname,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address
            };

            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            var before = await _customerService.GetCustomerByIdAsync(id);
            var updated = await _customerService.UpdateCustomerAsync(id, dto);

            if (updated == null) return NotFound();

            await _audit.WriteAsync(
                AuditEvent.Account.UpdateProfile, "Customer", id.ToString(),
                beforeJson: System.Text.Json.JsonSerializer.Serialize(before),
                afterJson: System.Text.Json.JsonSerializer.Serialize(dto),
                criticalValue: "Fields=Username,Name,Surname,Email,Phone,Address");

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPut("{id}/update-password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { message = "New password and confirmation do not match" });

            var success = await _customerService.UpdatePasswordAsync(id, dto.CurrentPassword, dto.NewPassword);

            if (success)
            {
                await _audit.WriteAsync(
                    AuditEvent.Account.UpdatePassword, "Customer", id.ToString(),
                    beforeJson: null, afterJson: null, criticalValue: "PasswordChanged=true");
                return Ok(new { message = "Password updated successfully" });
            }

            return BadRequest(new { message = "Current password is incorrect or user not found" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var result = await _customerService.DeleteCustomerAsync(id);
            if (result)
            {
                await _audit.WriteAsync(
                    AuditEvent.Account.Delete, "Customer", id.ToString(),
                    null, null, "Result=SoftDeleteApplied_OrdersPreserved");
                return Ok(new { message = "Customer account deleted successfully. All order history preserved." });
            }
            return NotFound();
        }
    }
}