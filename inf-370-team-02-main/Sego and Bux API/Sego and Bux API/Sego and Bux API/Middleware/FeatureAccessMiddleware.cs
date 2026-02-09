// Sego_and__Bux/Middleware/FeatureAccessMiddleware.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Middleware
{
    /// <summary>
    /// Extra safety net: if admin has restricted a feature, block API calls even if
    /// a controller's static [Authorize] would allow them.
    /// This NEVER grants extra access; it only denies when admin rules say so.
    /// </summary>
    public class FeatureAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFeatureAccessService _features;

        // Map route prefixes to feature keys (aligns with your controllers)
        private static readonly (PathString Prefix, string FeatureKey)[] Map = new[]
        {
            (new PathString("/api/reports"),               "reports"),
            (new PathString("/api/admin/stock"),           "stock"),
            (new PathString("/api/Product"),               "products"),       // ProductController assumed
            (new PathString("/api/ProductType"),           "productTypes"),
            (new PathString("/api/Category"),              "categories"),
            (new PathString("/api/category"),              "categories"),     // ADDED: lowercase version
            (new PathString("/api/Employee"),              "employees"),      // EmployeeController assumed
            (new PathString("/api/Order"),                 "orders"),         // OrderController assumed
            (new PathString("/api/admin/productreview"),   "productReviews"),
            (new PathString("/api/feedback"),              "customerFeedback"),
            (new PathString("/api/Template"),              "templates"),
            (new PathString("/api/vat"),                   "vat"),
            (new PathString("/api/admin/faq"),             "faqs"),
            (new PathString("/api/admin/chatbot-config"),  "chatbotConfig"),
            (new PathString("/api/metrics"),               "dashboard"),      // charts on dashboard
            (new PathString("/api/audit"),                 "dashboard"),      // audit lives on dashboard screen
        };

        public FeatureAccessMiddleware(RequestDelegate next, IFeatureAccessService features)
        {
            _next = next;
            _features = features;
        }

        public async Task Invoke(HttpContext ctx)
        {
            // Allow anonymous/static files/etc shortcuts
            var path = ctx.Request.Path;

            // Allow the management endpoints themselves (handled by [Authorize(Roles="Admin")])
            if (path.StartsWithSegments("/api/admin/feature-access", out _))
            {
                await _next(ctx);
                return;
            }

            var match = Map.FirstOrDefault(m => path.StartsWithSegments(m.Prefix, out _));
            if (string.IsNullOrEmpty(match.FeatureKey))
            {
                await _next(ctx);
                return; // not a managed feature
            }

            // If not authenticated yet, let the normal auth flow handle 401/403
            if (ctx.User?.Identity?.IsAuthenticated != true)
            {
                await _next(ctx);
                return;
            }

            if (!_features.IsUserAllowedForFeature(ctx.User, match.FeatureKey))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    message = "Access to this feature is restricted by admin configuration.",
                    feature = match.FeatureKey
                });
                return;
            }

            await _next(ctx);
        }
    }
}