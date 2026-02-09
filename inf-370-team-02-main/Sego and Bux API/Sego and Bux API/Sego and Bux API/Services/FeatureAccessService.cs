// Sego_and__Bux/Services/FeatureAccessService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Services
{
    /// <summary>
    /// Stores feature access in App_Data/feature-access.json (no DB migration needed).
    /// Applies defaults when file doesn't exist.
    /// </summary>
    public class FeatureAccessService : IFeatureAccessService
    {
        private readonly string _filePath;
        private static readonly SemaphoreSlim _gate = new(1, 1);

        // The four roles you already use
        private static readonly string[] AllRoles = new[] { "Admin", "Manager", "Employee", "Customer" };

        // Feature catalog used by UI + middleware + defaults
        private static readonly (string Key, string Name)[] CatalogItems = new[]
        {
            ("dashboard",       "Dashboard"),
            ("reports",         "Reports"),
            ("stock",           "Stock Manager"),
            ("products",        "Products"),
            ("productTypes",    "Product Types"),
            ("categories",      "Categories"),
            ("employees",       "Employees"),
            ("orders",          "Orders"),
            ("productReviews",  "Product Reviews"),
            ("customerFeedback","Customer Feedback"),
            ("templates",       "Templates"),
            ("vat",             "VAT"),
            ("faqs",            "FAQs"),
            ("chatbotConfig",   "Chatbot Config")
        };

        // Defaults chosen to MATCH your current [Authorize] usage, so nothing breaks.
        private static readonly Dictionary<string, string[]> DefaultRoles = new()
        {
            // Admin pages
            ["dashboard"] = new[] { "Admin", "Manager", "Employee" },
            ["reports"] = new[] { "Admin", "Manager", "Employee" },
            ["stock"] = new[] { "Admin", "Manager", "Employee" }, // InventoryStaff policy
            ["products"] = new[] { "Admin", "Manager", "Employee" },
            ["productTypes"] = new[] { "Admin", "Manager", "Employee" },
            ["categories"] = new[] { "Admin", "Manager", "Employee" },
            ["employees"] = new[] { "Admin", "Manager", "Employee" }, // keep broad as per your menu
            ["orders"] = new[] { "Admin", "Manager", "Employee" },
            ["productReviews"] = new[] { "Admin", "Manager", "Employee" },
            ["customerFeedback"] = new[] { "Admin", "Manager", "Employee" },
            ["templates"] = new[]  {"Admin", "Manager", "Employee" },
            ["vat"] = new[] { "Admin", "Manager", "Employee" },
            ["faqs"] = new[] { "Admin", "Manager", "Employee" },
            ["chatbotConfig"] = new[] { "Admin", "Manager", "Employee" }
        };

        public FeatureAccessService(IHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "feature-access.json");
        }

        public IReadOnlyList<(string Key, string DisplayName)> Catalog
            => CatalogItems.ToList();

        public async Task<IReadOnlyList<FeatureAccessDto>> GetEffectiveAsync()
        {
            var map = await ReadAsync();

            // ensure we always return every item from the catalog
            return CatalogItems
                .Select(ci => new FeatureAccessDto
                {
                    Key = ci.Key,
                    DisplayName = ci.Name,
                    Roles = map.TryGetValue(ci.Key, out var roles) ? roles : DefaultRoles[ci.Key]
                })
                .ToList();
        }

        public async Task UpsertBulkAsync(IEnumerable<FeatureAccessDto> items, string updatedBy)
        {
            var incoming = items?.ToDictionary(x => x.Key, x => x.Roles ?? Array.Empty<string>())
                           ?? new Dictionary<string, string[]>();

            await _gate.WaitAsync();
            try
            {
                var current = await ReadAsync();
                foreach (var kvp in incoming)
                {
                    // sanity: only known roles, unique + sorted
                    var cleaned = kvp.Value.Where(r => AllRoles.Contains(r))
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .OrderBy(r => Array.IndexOf(AllRoles, r))
                                           .ToArray();
                    current[kvp.Key] = cleaned;
                }
                await WriteAsync(current);
            }
            finally
            {
                _gate.Release();
            }
        }

        public bool IsUserAllowedForFeature(ClaimsPrincipal user, string featureKey)
        {
            var roles = GetUserRoles(user);
            var effective = GetEffectiveAsync().GetAwaiter().GetResult()
                .FirstOrDefault(x => x.Key == featureKey)?.Roles ?? Array.Empty<string>();
            return roles.Overlaps(effective);
        }

        public async Task<HashSet<string>> GetAllowedFeaturesForUserAsync(ClaimsPrincipal user)
        {
            var roles = GetUserRoles(user);
            var eff = await GetEffectiveAsync();
            return eff.Where(e => roles.Overlaps(e.Roles)).Select(e => e.Key).ToHashSet();
        }

        // ---------- helpers ----------
        private static HashSet<string> GetUserRoles(ClaimsPrincipal user)
        {
            var claims = user?.Claims?.Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                            .Select(c => c.Value)
                            .ToArray() ?? Array.Empty<string>();
            return new HashSet<string>(claims, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<string, string[]>> ReadAsync()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            await using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var map = await JsonSerializer.DeserializeAsync<Dictionary<string, string[]>>(fs,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new Dictionary<string, string[]>();
            return new Dictionary<string, string[]>(map, StringComparer.OrdinalIgnoreCase);
        }

        private async Task WriteAsync(Dictionary<string, string[]> map)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            await using var fs = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fs, map, opts);
        }
    }

    internal static class SetOps
    {
        public static bool Overlaps(this HashSet<string> set, IEnumerable<string> other)
            => other != null && other.Any(r => set.Contains(r));
    }
}
