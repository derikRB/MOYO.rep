using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sego_and__Bux.Services
{
    public class ChatbotConfigService : IChatbotConfigService
    {
        private readonly ApplicationDbContext _db;
        public ChatbotConfigService(ApplicationDbContext db) => _db = db;

        public async Task<ChatbotConfigDto> GetAsync()
        {
            var cfg = await _db.ChatbotConfigs.FirstOrDefaultAsync();
            if (cfg == null) return null;

            return new ChatbotConfigDto
            {
                Id = cfg.Id,
                WhatsAppNumber = cfg.WhatsAppNumber,
                SupportEmail = cfg.SupportEmail
            };
        }

        public async Task<ChatbotConfigDto> UpdateAsync(ChatbotConfigDto dto)
        {
            var cfg = await _db.ChatbotConfigs.FirstOrDefaultAsync();
            if (cfg == null) return null;

            // --- International number validation/formatting ---
            string number = dto.WhatsAppNumber.Trim();
            // Remove spaces, dashes, and plus sign
            number = number.Replace(" ", "").Replace("-", "").Replace("+", "");
            // Convert local SA number (e.g., 065...) to 27...
            if (number.StartsWith("0") && number.Length == 10)
                number = "27" + number.Substring(1);

            cfg.WhatsAppNumber = number;
            cfg.SupportEmail = dto.SupportEmail.Trim();

            await _db.SaveChangesAsync();

            return new ChatbotConfigDto
            {
                Id = cfg.Id,
                WhatsAppNumber = cfg.WhatsAppNumber,
                SupportEmail = cfg.SupportEmail
            };
        }

    }
}
