using System.Collections.Generic;
using System.Security.Claims;

namespace Sego_and__Bux.Interfaces
{
    public interface IJwtService
    {
        // Original
        string GenerateToken(string userId, IList<string> roles);

        // Overload to support username/email (add this!)
        string GenerateToken(string userId, IList<string> roles, string username = null, string email = null);

        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
