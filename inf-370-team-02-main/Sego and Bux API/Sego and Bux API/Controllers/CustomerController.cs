using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using System.Threading.Tasks;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            var dto = new CustomerDto
            {
                Id = customer.Id, // This is CustomerID mapped to DTO Id
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
            var updated = await _customerService.UpdateCustomerAsync(id, dto);
            return updated == null
                ? NotFound()
                : Ok(new { message = "Profile updated successfully" });
        }

        [HttpPut("{id}/update-password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return BadRequest(new { message = "New password and confirmation do not match" });
            }

            var success = await _customerService.UpdatePasswordAsync(id, dto.CurrentPassword, dto.NewPassword);

            return success
                ? Ok(new { message = "Password updated successfully" })
                : BadRequest(new { message = "Current password is incorrect or user not found" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var result = await _customerService.DeleteCustomerAsync(id);
            return result
                ? Ok(new { message = "Deleted" })
                : NotFound();
        }
    }
}
