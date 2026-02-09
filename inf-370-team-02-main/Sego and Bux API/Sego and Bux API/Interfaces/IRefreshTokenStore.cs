namespace Sego_and__Bux.Interfaces
{
    public interface IRefreshTokenStore
    {
        void SaveRefreshToken(string userId, string refreshToken);
        bool ValidateRefreshToken(string userId, string refreshToken);
        void RemoveRefreshToken(string userId, string refreshToken);
        void RemoveAllRefreshTokens(string userId);
    }

}
