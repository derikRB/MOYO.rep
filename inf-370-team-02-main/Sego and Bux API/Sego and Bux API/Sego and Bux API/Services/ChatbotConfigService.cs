using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Services
{
    public class ChatbotConfigService : IChatbotConfigService
    {
        private readonly ApplicationDbContext _db;
        public ChatbotConfigService(ApplicationDbContext db) => _db = db;

        public async Task<ChatbotConfigDto?> GetAsync()
        {
            var cfg = await _db.ChatbotConfigs.AsNoTracking().FirstOrDefaultAsync();
            if (cfg == null) return null;

            return new ChatbotConfigDto
            {
                Id = cfg.Id,
                WhatsAppNumber = cfg.WhatsAppNumber,
                SupportEmail = cfg.SupportEmail,
                CompanyAddress = cfg.CompanyAddress,
                ThresholdKm = cfg.DeliveryRadiusKm,
                FlatShippingFee = cfg.CourierFlatFee,
                HandToHandFee = cfg.HandToHandFee
            };
        }

        public async Task<ChatbotConfigDto?> UpdateAsync(ChatbotConfigDto dto)
        {
            var cfg = await _db.ChatbotConfigs.FirstOrDefaultAsync();
            if (cfg == null)
            {
                cfg = new ChatbotConfig();
                _db.ChatbotConfigs.Add(cfg);
            }

            // normalize ZA numbers (27…)
            var num = (dto.WhatsAppNumber ?? "").Replace(" ", "").Replace("-", "").Replace("+", "");
            if (num.Length == 10 && num[0] == '0') num = "27" + num.Substring(1);

            cfg.WhatsAppNumber = num;
            cfg.SupportEmail = dto.SupportEmail?.Trim() ?? "";
            cfg.CompanyAddress = dto.CompanyAddress?.Trim() ?? "";
            cfg.DeliveryRadiusKm = dto.ThresholdKm <= 0 ? 1 : dto.ThresholdKm;
            cfg.CourierFlatFee = dto.FlatShippingFee < 0 ? 0 : dto.FlatShippingFee;
            cfg.HandToHandFee = dto.HandToHandFee < 0 ? 0 : dto.HandToHandFee;

            await _db.SaveChangesAsync();

            return new ChatbotConfigDto
            {
                Id = cfg.Id,
                WhatsAppNumber = cfg.WhatsAppNumber,
                SupportEmail = cfg.SupportEmail,
                CompanyAddress = cfg.CompanyAddress,
                ThresholdKm = cfg.DeliveryRadiusKm,
                FlatShippingFee = cfg.CourierFlatFee,
                HandToHandFee = cfg.HandToHandFee
            };
        }
    }
}
