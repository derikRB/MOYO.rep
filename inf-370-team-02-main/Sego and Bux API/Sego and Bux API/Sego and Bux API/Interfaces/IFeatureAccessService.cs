// Sego_and__Bux/Interfaces/IFeatureAccessService.cs
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface IFeatureAccessService
    {
        Task<IReadOnlyList<FeatureAccessDto>> GetEffectiveAsync();
        Task UpsertBulkAsync(IEnumerable<FeatureAccessDto> items, string updatedBy);
        Task<HashSet<string>> GetAllowedFeaturesForUserAsync(ClaimsPrincipal user);
        bool IsUserAllowedForFeature(ClaimsPrincipal user, string featureKey);
        IReadOnlyList<(string Key, string DisplayName)> Catalog { get; }
    }
}
