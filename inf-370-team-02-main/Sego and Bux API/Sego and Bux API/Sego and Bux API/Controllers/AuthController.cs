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
using Sego_and__Bux.Helpers;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Services;
using Sego_and__Bux.Audit;

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
        private readonly IAppConfigService _appConfig;
        private readonly IAuditWriter _audit;

        public AuthController(
            ApplicationDbContext db,
            ICustomerService customerService,
            IEmployeeService employeeService,
            IJwtService jwtService,
            IRefreshTokenStore refreshTokenStore,
            EmailSender emailSender,
            IAppConfigService appConfig,
            IAuditWriter audit)
        {
            _db = db;
            _customerService = customerService;
            _employeeService = employeeService;
            _jwtService = jwtService;
            _refreshTokenStore = refreshTokenStore;
            _emailSender = emailSender;
            _appConfig = appConfig;
            _audit = audit;
        }

        [HttpPost("customer/register")]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerDto dto)
        {
            if (await _customerService.ExistsByUsernameOrEmailAsync(dto.Username, dto.Email))
                return BadRequest(new { message = "Username or Email already exists." });

            string otp = GenerateOtp();

            var customer = await _customerService.RegisterAsync(dto);
            customer.IsVerified = false;
            customer.OtpCode = otp;

            var otpMinutes = await _appConfig.GetIntAsync(AppConfigService.OtpKey, 10);
            customer.OtpExpiry = DateTime.UtcNow.AddMinutes(otpMinutes);

            await _db.SaveChangesAsync();
            await _emailSender.SendOtpAsync(dto.Email, otp);

            // AUDIT: registration (pre-auth)
            await _audit.WriteForUserAsync(
                customer.CustomerID, null, customer.Email, customer.Username,
                AuditEvent.Auth.Register, "Customer", customer.CustomerID.ToString(),
                null, null, $"Email={dto.Email}");

            return Ok(new { message = "OTP sent—please verify with the code emailed to you." });
        }

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

                // AUDIT
                await _audit.WriteForUserAsync(
                    c.CustomerID, null, c.Email, c.Username,
                    AuditEvent.Auth.VerifyOtp, "Customer", c.CustomerID.ToString(),
                    null, null, "Result=Success");

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

        [HttpPost("customer/resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] OtpResendDto dto)
        {
            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == dto.Email && !x.IsVerified);
            if (c == null)
                return NotFound(new { message = "User not found or already verified." });

            string otp = GenerateOtp();
            c.OtpCode = otp;

            var otpMinutes2 = await _appConfig.GetIntAsync(AppConfigService.OtpKey, 10);
            c.OtpExpiry = DateTime.UtcNow.AddMinutes(otpMinutes2);

            await _db.SaveChangesAsync();
            await _emailSender.SendOtpAsync(dto.Email, otp);

            // AUDIT
            await _audit.WriteForUserAsync(
                c.CustomerID, null, c.Email, c.Username,
                AuditEvent.Auth.ResendOtp, "Customer", c.CustomerID.ToString(),
                null, null, "Result=Success");

            return Ok(new { message = "New OTP sent." });
        }

        [HttpPost("customer/login")]
        public async Task<IActionResult> LoginCustomer([FromBody] LoginDto dto)
        {
            var c = await _customerService.GetCustomerByUsernameOrEmailAsync(dto.EmailOrUsername);
            if (c == null || !PasswordHasher.VerifyPassword(dto.Password, c.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });
            if (!c.IsVerified)
                return Unauthorized(new { message = "Please verify your email before logging in." });

            var token = _jwtService.GenerateToken(c.CustomerID.ToString(), new[] { "Customer" });
            var refreshToken = _jwtService.GenerateRefreshToken();
            _refreshTokenStore.SaveRefreshToken(c.CustomerID.ToString(), refreshToken);

            // AUDIT
            await _audit.WriteForUserAsync(
                c.CustomerID, null, c.Email, c.Username,
                AuditEvent.Auth.Login, "Customer", c.CustomerID.ToString(),
                null, null, "Result=Success");

            return Ok(new RefreshResponseDto { Token = token, RefreshToken = refreshToken });
        }

        [HttpPost("employee/login")]
        public async Task<IActionResult> LoginEmployee(LoginDto dto)
        {
            var employee = (await _employeeService.GetAllEmployeesAsync())
                .FirstOrDefault(e => (e.Username == dto.EmailOrUsername || e.Email == dto.EmailOrUsername) && e.IsActive); // ← ADD e.IsActive check

            if (employee == null)
                return Unauthorized(new { message = "Invalid credentials" });

            bool passwordValid;
            try { passwordValid = PasswordHasher.VerifyPassword(dto.Password, employee.PasswordHash); }
            catch { passwordValid = dto.Password == employee.PasswordHash; }

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

            // AUDIT
            await _audit.WriteForUserAsync(
                null, employee.EmployeeID, employee.Email, employee.Username,
                AuditEvent.Auth.Login, "Employee", employee.EmployeeID.ToString(),
                null, null, $"Result=Success; Role={employee.Role}");

            return Ok(new RefreshResponseDto { Token = token, RefreshToken = refreshToken });
        }

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

                // Optional: log refresh if required
                // _audit.WriteAsync(AuditEvent.Auth.Refresh, "Token", null, null, null, "Result=Success").GetAwaiter().GetResult();

                return Ok(new RefreshResponseDto { Token = newJwt, RefreshToken = newRt });
            }
            catch (SecurityTokenException)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
        }

        [HttpGet("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken([FromQuery] string email, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Invalid or expired reset token." });

            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == email);
            if (c == null)
                return BadRequest(new { message = "Invalid or expired reset token." });

            if (string.IsNullOrEmpty(c.OtpCode) || c.OtpCode != token || c.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Invalid or expired reset token." });

            return Ok(new { message = "Token ok." });
        }

        // ---------- Password reset ----------

        public class ForgotPasswordRequest { public string Email { get; set; } = default!; }
        public class ResetPasswordRequest
        {
            public string Email { get; set; } = default!;
            public string Token { get; set; } = default!;  // reuse OTP columns
            public string NewPassword { get; set; } = default!;
            public string ConfirmPassword { get; set; } = default!;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "Email is required." });

            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == req.Email);

            // Always respond 200 (no enumeration)
            if (c == null) return Ok();

            string token = GenerateOtp();
            var minutes = await _appConfig.GetIntAsync(AppConfigService.OtpKey, 10);
            c.OtpCode = token;
            c.OtpExpiry = DateTime.UtcNow.AddMinutes(minutes);
            await _db.SaveChangesAsync();

            var origin = Request.Headers["Origin"].FirstOrDefault() ?? "http://localhost:4200";
            var link = $"{origin}/auth/reset-password?email={Uri.EscapeDataString(req.Email)}&token={Uri.EscapeDataString(token)}";

            await _emailSender.SendPasswordResetAsync(req.Email, link);

            // AUDIT
            await _audit.WriteForUserAsync(
                c.CustomerID, null, c.Email, c.Username,
                AuditEvent.Auth.ForgotPassword, "Customer", c.CustomerID.ToString(),
                null, null, "Result=IssuedToken");

            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Token) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { message = "Invalid request." });

            if (req.NewPassword != req.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            var c = await _db.Customers.SingleOrDefaultAsync(x => x.Email == req.Email);
            // enumeration-safe
            if (c == null) return Ok();

            if (string.IsNullOrEmpty(c.OtpCode) || c.OtpCode != req.Token || c.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Invalid or expired reset token." });

            c.PasswordHash = PasswordHasher.HashPassword(req.NewPassword);
            c.OtpCode = null;
            c.OtpExpiry = null;
            await _db.SaveChangesAsync();

            // AUDIT
            await _audit.WriteForUserAsync(
                c.CustomerID, null, c.Email, c.Username,
                AuditEvent.Auth.ResetPassword, "Customer", c.CustomerID.ToString(),
                null, null, "Result=Success");

            return Ok(new { message = "Password reset successful." });
        }

        // ---------- helpers ----------
        private static string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int code = Math.Abs(BitConverter.ToInt32(bytes, 0) % 1_000_000);
            return code.ToString("D6");
        }
    }

    public class OtpVerifyDto { public string Email { get; set; } = default!; public string Otp { get; set; } = default!; }
    public class OtpResendDto { public string Email { get; set; } = default!; }
}
