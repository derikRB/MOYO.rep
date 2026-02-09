
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Helpers;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;

namespace Sego_and__Bux.Services
{
    public class CustomizationService : ICustomizationService
    {
        private readonly ApplicationDbContext _context;
        public CustomizationService(ApplicationDbContext context) => _context = context;

        public async Task<Customization> AddCustomizationAsync(CustomizationDto dto)
        {
            var customization = new Customization
            {
                OrderLineID = dto.OrderLineID,
                Template = dto.Template,
                CustomText = dto.CustomText,
                Font = dto.Font,
                FontSize = dto.FontSize,
                Color = dto.Color,
                UploadedImagePath = dto.UploadedImagePath
            };
            _context.Customizations.Add(customization);
            await _context.SaveChangesAsync();
            return customization;
        }

        public async Task<bool> DeleteCustomizationAsync(int orderLineId)
        {
            var customization = await _context.Customizations.FirstOrDefaultAsync(c => c.OrderLineID == orderLineId);
            if (customization == null) return false;
            _context.Customizations.Remove(customization);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Customization?> GetByOrderLineIdAsync(int orderLineId) =>
            await _context.Customizations.Include(c => c.OrderLine).ThenInclude(ol => ol.Product).FirstOrDefaultAsync(c => c.OrderLineID == orderLineId);

        public async Task<Customization> UpdateCustomizationAsync(int orderLineId, CustomizationDto dto)
        {
            var c = await _context.Customizations.FirstOrDefaultAsync(c => c.OrderLineID == orderLineId);
            if (c == null) return null!;
            c.Template = dto.Template;
            c.CustomText = dto.CustomText;
            c.Font = dto.Font;
            c.FontSize = dto.FontSize;
            c.Color = dto.Color;
            c.UploadedImagePath = dto.UploadedImagePath;
            await _context.SaveChangesAsync();
            return c;
        }
    }

}
