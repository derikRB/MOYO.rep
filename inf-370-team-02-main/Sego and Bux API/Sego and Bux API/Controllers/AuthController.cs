using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.DTOs.Password;
using Sego_and__Bux.Helpers;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Services;

namespace Sego_and__Bux.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICustomerService _customerService;
        private readonly IEmployeeService _employeeService;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly EmailSender _emailSender;

        public AuthController(
            ApplicationDbContext db,
            ICustomerService customerService,
            IEmployeeService employeeService,
            IJwtService jwtService,
            IRefreshTokenStore refreshTokenStore,
            EmailSender emailSender)
        {
            _db = db;
            _customerService = customerService;
            _employeeService = employeeService;
            _jwtService = jwtService;
            _refreshTokenStore = refreshTokenStore;
            _emailSender = emailSender;
        }

        // ─── CUSTOMER REGISTER (Step 1: Registration with OTP) ────────────────
        [HttpPost("customer/register")]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerDto dto)
        {
            if (await _customerService.ExistsByUsernameOrEmailAsync(dto.Username, dto.Email))
                return BadRequest(new { message = "Username or Email already exists." });

            string otp = GenerateOtp();

            var customer = await _customerService.RegisterAsync(dto);
            customer.IsVerified = false;
            customer.OtpCode = otp;
            customer.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            await _db.SaveChangesAsync();
            await _emailSender.SendOtpAsync(dto.Email, otp);

            return Ok(new { message = "OTP sent—please verify with the code emailed to you." });
        }

        // ─── CUSTOMER VERIFY OTP (Step 2: Confirm Email) ─────────────────────
        [HttpPost("customer/verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto dto)
        {
            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == dto.Email);
            if (c == null)
                return NotFound(new { message = "User not found." });
            if (c.IsVerified)
                return BadRequest(new { message = "Already verified." });

            if (c.OtpCode == dto.Otp && c.OtpExpiry >= DateTime.UtcNow)
            {
                c.IsVerified = true;
                c.OtpCode = null;
                c.OtpExpiry = null;
                await _db.SaveChangesAsync();

                var token = _jwtService.GenerateToken(c.CustomerID.ToString(), new[] { "Customer" });
                var refreshToken = _jwtService.GenerateRefreshToken();
                _refreshTokenStore.SaveRefreshToken(c.CustomerID.ToString(), refreshToken);

                return Ok(new
                {
                    message = "Email verified—registration complete.",
                    token,
                    refreshToken
                });
            }
            return BadRequest(new { message = "Invalid or expired OTP." });
        }

        // ─── RESEND OTP ─────────────────────────────────────────────────────
        [HttpPost("customer/resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] OtpResendDto dto)
        {
            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == dto.Email && !x.IsVerified);
            if (c == null)
                return NotFound(new { message = "User not found or already verified." });

            string otp = GenerateOtp();
            c.OtpCode = otp;
            c.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            await _emailSender.SendOtpAsync(dto.Email, otp);
            return Ok(new { message = "New OTP sent." });
        }

        // ─── CUSTOMER LOGIN ────────────────────────────────────────────────
        [HttpPost("customer/login")]
        public async Task<IActionResult> LoginCustomer([FromBody] LoginDto dto)
        {
            var c = await _customerService.GetCustomerByUsernameOrEmailAsync(dto.EmailOrUsername);
            if (c == null || !PasswordHasher.VerifyPassword(dto.Password, c.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            // If not verified, block login except if password was just reset (i.e. IsVerified = true now)
            if (!c.IsVerified)
                return Unauthorized(new { message = "Please verify your email before logging in." });

            var token = _jwtService.GenerateToken(c.CustomerID.ToString(), new[] { "Customer" });
            var refreshToken = _jwtService.GenerateRefreshToken();
            _refreshTokenStore.SaveRefreshToken(c.CustomerID.ToString(), refreshToken);

            return Ok(new RefreshResponseDto { Token = token, RefreshToken = refreshToken });
        }

        // ─── EMPLOYEE LOGIN ────────────────────────────────────────────────
        [HttpPost("employee/login")]
        public async Task<IActionResult> LoginEmployee(LoginDto dto)
        {
            var employee = (await _employeeService.GetAllEmployeesAsync())
                .FirstOrDefault(e =>
                    e.Username == dto.EmailOrUsername ||
                    e.Email == dto.EmailOrUsername);

            if (employee == null)
                return Unauthorized(new { message = "Invalid credentials" });

            bool passwordValid;
            try
            {
                passwordValid = PasswordHasher.VerifyPassword(dto.Password, employee.PasswordHash);
            }
            catch
            {
                passwordValid = dto.Password == employee.PasswordHash;
            }

            if (!passwordValid)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = _jwtService.GenerateToken(
                employee.EmployeeID.ToString(),
                new[] { employee.Role },
                employee.Username,
                employee.Email
            );
            var refreshToken = _jwtService.GenerateRefreshToken();
            _refreshTokenStore.SaveRefreshToken(employee.EmployeeID.ToString(), refreshToken);

            return Ok(new RefreshResponseDto
            {
                Token = token,
                RefreshToken = refreshToken
            });
        }

        // ─── REFRESH TOKEN ────────────────────────────────────────────────
        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] RefreshRequestDto req)
        {
            try
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(req.Token);
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var roles = principal.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                if (!_refreshTokenStore.ValidateRefreshToken(userId, req.RefreshToken))
                    return Unauthorized(new { message = "Invalid refresh token" });

                var newJwt = _jwtService.GenerateToken(userId, roles);
                var newRt = _jwtService.GenerateRefreshToken();
                _refreshTokenStore.RemoveRefreshToken(userId, req.RefreshToken);
                _refreshTokenStore.SaveRefreshToken(userId, newRt);

                return Ok(new RefreshResponseDto { Token = newJwt, RefreshToken = newRt });
            }
            catch (SecurityTokenException)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
        }

        // ─── FORGOT PASSWORD ──────────────────────────────────────────────
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == dto.Email);
            // Do not reveal user existence
            if (c == null)
                return Ok(new { message = "If this email exists, password reset instructions have been sent." });

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            c.PasswordResetToken = token;
            c.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync();

            var resetLink = $"http://localhost:4200/auth/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";
            await _emailSender.SendPasswordResetAsync(dto.Email, resetLink);

            return Ok(new { message = "If this email exists, password reset instructions have been sent." });
        }

        // ─── RESET PASSWORD ────────────────────────────────────────────────
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                return BadRequest(new { message = "All password fields are required." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == dto.Email);
            if (c == null || c.PasswordResetToken != dto.Token
                || c.PasswordResetTokenExpiry == null
                || c.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Invalid or expired reset token." });
            }

            c.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);
            c.IsVerified = true; // Ensure that reset also verifies if not already
            c.PasswordResetToken = null;
            c.PasswordResetTokenExpiry = null;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Password reset successful. You may now log in with your new password." });
        }

        // ─── OTP Helper ────────────────────────────────────────────────────
        private static string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int code = Math.Abs(BitConverter.ToInt32(bytes, 0) % 1_000_000);
            return code.ToString("D6");
        }
    }

    // ─── OTP DTOs ────────────────────────────────────────────────────────
    public class OtpVerifyDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
    public class OtpResendDto
    {
        public string Email { get; set; }
    }
}
