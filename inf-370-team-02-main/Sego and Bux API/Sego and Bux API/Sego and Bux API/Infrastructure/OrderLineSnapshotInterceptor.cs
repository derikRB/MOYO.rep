using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sego_and__Bux.Data;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class OrderLineSnapshotInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not ApplicationDbContext db)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            // snapshot entries to avoid "Collection was modified"
            var entries = db.ChangeTracker.Entries<OrderLine>()
                .Where(x => x.State is EntityState.Added or EntityState.Modified)
                .ToList();

            // Default VAT rate (your Product doesn't have VatId)
            decimal? defaultVatRate = db.Vats
                .OrderByDescending(v => v.EffectiveDate)
                .Select(v => (decimal?)v.Percentage)
                .FirstOrDefault();

            foreach (var e in entries)
            {
                // quick helper: only write if the shadow property exists in the model
                bool Has(string name) => e.Metadata.FindProperty(name) is not null;

                var product = e.Entity.Product
                              ?? db.Products.Local.FirstOrDefault(p => p.ProductID == e.Entity.ProductID)
                              ?? db.Products.AsNoTracking().FirstOrDefault(p => p.ProductID == e.Entity.ProductID);

                var unitPrice = product?.Price;

                if (Has("UnitPriceAtSale"))
                    e.Property("UnitPriceAtSale").CurrentValue = unitPrice;

                if (Has("VatRateAtSale"))
                    e.Property("VatRateAtSale").CurrentValue = defaultVatRate;

                if (Has("ProductNameSnapshot"))
                    e.Property("ProductNameSnapshot").CurrentValue = product?.Name;

                // Your Product model doesn’t have SKU; keep null unless you add it later
                if (Has("SkuSnapshot") && e.Property("SkuSnapshot").CurrentValue is null)
                    e.Property("SkuSnapshot").CurrentValue = null;

                if (Has("TemplateVersion") && e.Property("TemplateVersion").CurrentValue is null)
                    e.Property("TemplateVersion").CurrentValue = "v1";

                // leave CustomizationJsonPath as-is if your flow sets it elsewhere
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
