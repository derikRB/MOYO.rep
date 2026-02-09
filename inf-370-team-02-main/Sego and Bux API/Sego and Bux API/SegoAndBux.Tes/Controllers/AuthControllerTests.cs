using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sego_and__Bux.Controllers;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Helpers;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Services;
using SegoAndBux.Tests.Common;
using SegoAndBux.Tests.Fakes;
using Xunit;

namespace SegoAndBux.Tests.Controllers
{
    public class AuthControllerTests
    {
        private static AuthController BuildController(
            ApplicationDbContext db,
            ICustomerService customerService,
            IEmployeeService employeeService,
            IJwtService jwtService,
            IRefreshTokenStore refreshStore)
        {
            // Controller signature: (db, customerSvc, employeeSvc, jwtSvc, refreshStore, EmailSender, IAppConfigService, IAuditWriter)
            EmailSender emailSender = null!; // not used in these tests
            var appCfg = Mock.Of<IAppConfigService>();
            var audit = Mock.Of<IAuditWriter>();

            return new AuthController(db, customerService, employeeService, jwtService, refreshStore, emailSender, appCfg, audit);
        }

        [Fact]
        public async Task LoginCustomer_ValidCredentials_ReturnsJwtAndRefresh()
        {
            var db = TestDb.NewContext();
            var customer = new Sego_and__Bux.Models.Customer
            {
                CustomerID = 42,
                Email = "e@x.com",
                Username = "u",
                PasswordHash = PasswordHasher.HashPassword("P@ssw0rd!"),
                IsVerified = true
            };

            var customerSvc = new Mock<ICustomerService>();
            customerSvc.Setup(s => s.GetCustomerByUsernameOrEmailAsync("u")).ReturnsAsync(customer);

            var jwt = new Mock<IJwtService>();
            jwt.Setup(j => j.GenerateToken("42", It.IsAny<IList<string>>())).Returns("jwt-123");
            jwt.Setup(j => j.GenerateRefreshToken()).Returns("rt-abc");

            var store = new FakeRefreshTokenStore();
            var sut = BuildController(db, customerSvc.Object, Mock.Of<IEmployeeService>(), jwt.Object, store);

            var result = await sut.LoginCustomer(new LoginDto { EmailOrUsername = "u", Password = "P@ssw0rd!" });

            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
            var payload = ok!.Value as RefreshResponseDto;
            payload!.Token.Should().Be("jwt-123");
            payload.RefreshToken.Should().Be("rt-abc");
            store.ValidateRefreshToken("42", "rt-abc").Should().BeTrue();
        }

        [Fact]
        public async Task LoginCustomer_Unverified_ReturnsUnauthorized()
        {
            var db = TestDb.NewContext();
            var c = new Sego_and__Bux.Models.Customer
            { CustomerID = 1, Email = "e@x.com", Username = "u", PasswordHash = PasswordHasher.HashPassword("x"), IsVerified = false };

            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerByUsernameOrEmailAsync("u")).ReturnsAsync(c);

            var sut = BuildController(db, svc.Object, Mock.Of<IEmployeeService>(), Mock.Of<IJwtService>(), new FakeRefreshTokenStore());

            var res = await sut.LoginCustomer(new LoginDto { EmailOrUsername = "u", Password = "x" });

            res.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task LoginCustomer_WrongPassword_ReturnsUnauthorized()
        {
            var db = TestDb.NewContext();
            var c = new Sego_and__Bux.Models.Customer
            { CustomerID = 1, Email = "e@x.com", Username = "u", PasswordHash = PasswordHasher.HashPassword("right"), IsVerified = true };

            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerByUsernameOrEmailAsync("u")).ReturnsAsync(c);

            var sut = BuildController(db, svc.Object, Mock.Of<IEmployeeService>(), Mock.Of<IJwtService>(), new FakeRefreshTokenStore());

            var res = await sut.LoginCustomer(new LoginDto { EmailOrUsername = "u", Password = "wrong" });

            res.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void Refresh_ValidOldTokenAndRT_IssuesNewPairAndRotatesStore()
        {
            var db = TestDb.NewContext();
            var jwt = new Mock<IJwtService>();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Role, "Customer")
            }, "test"));

            jwt.Setup(j => j.GetPrincipalFromExpiredToken("old-jwt")).Returns(principal);
            jwt.Setup(j => j.GenerateToken("42", It.IsAny<IList<string>>())).Returns("new-jwt");
            jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-rt");

            var store = new FakeRefreshTokenStore();
            store.SaveRefreshToken("42", "old-rt");

            var sut = BuildController(db, Mock.Of<ICustomerService>(), Mock.Of<IEmployeeService>(), jwt.Object, store);

            var result = sut.Refresh(new RefreshRequestDto { Token = "old-jwt", RefreshToken = "old-rt" });

            var ok = result as OkObjectResult;
            var payload = ok!.Value as RefreshResponseDto;
            payload!.Token.Should().Be("new-jwt");
            payload.RefreshToken.Should().Be("new-rt");
            store.ValidateRefreshToken("42", "old-rt").Should().BeFalse();
            store.ValidateRefreshToken("42", "new-rt").Should().BeTrue();
        }

        [Fact]
        public void Refresh_InvalidRefreshToken_ReturnsUnauthorized()
        {
            var db = TestDb.NewContext();
            var jwt = new Mock<IJwtService>();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Role, "Customer")
            }, "test"));
            jwt.Setup(j => j.GetPrincipalFromExpiredToken("old-jwt")).Returns(principal);

            var store = new FakeRefreshTokenStore();
            var sut = BuildController(db, Mock.Of<ICustomerService>(), Mock.Of<IEmployeeService>(), jwt.Object, store);

            var res = sut.Refresh(new RefreshRequestDto { Token = "old-jwt", RefreshToken = "invalid" });

            res.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
