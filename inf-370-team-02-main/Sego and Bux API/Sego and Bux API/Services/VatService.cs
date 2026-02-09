using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.Dto;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sego_and__Bux.Services
{
    public class VatService : IVatService
    {
        private readonly ApplicationDbContext _db;
        public VatService(ApplicationDbContext db) => _db = db;

        public async Task<List<Vat>> GetAllAsync() =>
            await _db.Vats.OrderByDescending(v => v.CreatedAt).ToListAsync();

        public async Task<Vat?> GetActiveAsync() =>
            await _db.Vats.FirstOrDefaultAsync(v => v.Status == "Active");

        public async Task<Vat> CreateAsync(VatDto dto)
        {
            // deactivate existing
            var current = await GetActiveAsync();
            if (current != null)
            {
                current.Status = "Inactive";
                current.LastUpdated = DateTime.UtcNow;
            }

            var vat = new Vat
            {
                VatName = dto.VatName,
                Percentage = dto.Percentage,
                EffectiveDate = dto.EffectiveDate,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _db.Vats.Add(vat);
            await _db.SaveChangesAsync();
            return vat;
        }

        public async Task<Vat?> UpdateAsync(int id, VatDto dto)
        {
            var vat = await _db.Vats.FindAsync(id);
            if (vat == null) return null;

            vat.VatName = dto.VatName;
            vat.Percentage = dto.Percentage;
            vat.EffectiveDate = dto.EffectiveDate;
            vat.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return vat;
        }

        public async Task<Vat?> ActivateAsync(int id)
        {
            var toActivate = await _db.Vats.FindAsync(id);
            if (toActivate == null) return null;

            // deactivate old
            var current = await GetActiveAsync();
            if (current != null && current.VatId != id)
            {
                current.Status = "Inactive";
                current.LastUpdated = DateTime.UtcNow;
            }

            toActivate.Status = "Active";
            toActivate.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return toActivate;
        }

        // --- Duplicates Logic ---
        public async Task<bool> ExistsByDateAsync(DateTime effectiveDate)
        {
            // Compare just the date, ignore time
            return await _db.Vats.AnyAsync(v => v.EffectiveDate.Date == effectiveDate.Date);
        }

        public async Task<bool> ExistsByDateForOtherIdAsync(DateTime effectiveDate, int excludeId)
        {
            // Ignore self for update
            return await _db.Vats.AnyAsync(v => v.EffectiveDate.Date == effectiveDate.Date && v.VatId != excludeId);
        }
    }
}
