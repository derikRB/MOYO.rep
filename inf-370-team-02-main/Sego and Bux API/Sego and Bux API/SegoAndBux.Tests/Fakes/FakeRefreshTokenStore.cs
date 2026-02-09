using System.Collections.Concurrent;
using Sego_and__Bux.Interfaces;

namespace SegoAndBux.Tests.Fakes
{
    public class FakeRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, string> _tokens = new();

        public void SaveRefreshToken(string userId, string refreshToken) => _tokens[userId] = refreshToken;

        public bool ValidateRefreshToken(string userId, string refreshToken) =>
            _tokens.TryGetValue(userId, out var existing) && existing == refreshToken;

        public void RemoveRefreshToken(string userId, string refreshToken)
        {
            if (_tokens.TryGetValue(userId, out var existing) && existing == refreshToken)
                _tokens.TryRemove(userId, out _);
        }

        public void RemoveAllRefreshTokens(string userId)
        {
            _tokens.TryRemove(userId, out _);
        }
    }
}
