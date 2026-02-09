using Sego_and__Bux.Helpers;
using Sego_and__Bux.Models;

namespace SegoAndBux.Tests.Common.Builders
{
    public class CustomerBuilder
    {
        private readonly Customer _c = new()
        {
            CustomerID = 101,
            Username = "cust1",
            Name = "Pink",
            Surname = "Panther",
            Email = "cust1@example.com",
            Phone = "000",
            Address = "123 Test St",
            IsVerified = true
        };

        public CustomerBuilder WithId(int id) { _c.CustomerID = id; return this; }
        public CustomerBuilder WithEmail(string email) { _c.Email = email; return this; }
        public CustomerBuilder Verified(bool v = true) { _c.IsVerified = v; return this; }
        public CustomerBuilder WithPassword(string raw)
        {
            _c.PasswordHash = PasswordHasher.HashPassword(raw);
            return this;
        }

        public Customer Build() => _c;
    }
}
