using System.Collections.Concurrent;
using Sego_and__Bux.Interfaces;  // Make sure you add this to reference the interface

namespace Sego_and__Bux.Services
{
    public class InMemoryRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userRefreshTokens = new();

        public void SaveRefreshToken(string userId, string refreshToken)
        {
            _userRefreshTokens.AddOrUpdate(userId,
                new HashSet<string> { refreshToken },
                (key, existingTokens) =>
                {
                    existingTokens.Add(refreshToken);
                    return existingTokens;
                });
        }

        public bool ValidateRefreshToken(string userId, string refreshToken)
        {
            return _userRefreshTokens.TryGetValue(userId, out var tokens) && tokens.Contains(refreshToken);
        }

        public void RemoveRefreshToken(string userId, string refreshToken)
        {
            if (_userRefreshTokens.TryGetValue(userId, out var tokens))
            {
                tokens.Remove(refreshToken);
                if (tokens.Count == 0)
                    _userRefreshTokens.TryRemove(userId, out _);
            }
        }

        public void RemoveAllRefreshTokens(string userId)
        {
            _userRefreshTokens.TryRemove(userId, out _);
        }
    }
}
