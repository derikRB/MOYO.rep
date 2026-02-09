using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Services
{
    public class FaqService : IFaqService
    {
        private readonly ApplicationDbContext _db;
        public FaqService(ApplicationDbContext db) => _db = db;

        public async Task<List<FaqItemDto>> GetAllAsync() =>
            await _db.FaqItems
              .OrderBy(f => f.SortOrder)
              .Select(f => new FaqItemDto
              {
                  FaqId = f.FaqId,
                  Category = f.Category,
                  QuestionVariant = f.QuestionVariant,
                  Answer = f.Answer,
                  SortOrder = f.SortOrder
              }).ToListAsync();

        public async Task<FaqItemDto> GetByIdAsync(int id) =>
            await _db.FaqItems
              .Where(f => f.FaqId == id)
              .Select(f => new FaqItemDto
              {
                  FaqId = f.FaqId,
                  Category = f.Category,
                  QuestionVariant = f.QuestionVariant,
                  Answer = f.Answer,
                  SortOrder = f.SortOrder
              }).FirstOrDefaultAsync();

        public async Task<FaqItemDto> CreateAsync(FaqItemDto dto)
        {
            var f = new FaqItem
            {
                Category = dto.Category,
                QuestionVariant = dto.QuestionVariant,
                Answer = dto.Answer,
                SortOrder = dto.SortOrder
            };
            _db.FaqItems.Add(f);
            await _db.SaveChangesAsync();
            dto.FaqId = f.FaqId;
            return dto;
        }

        public async Task<FaqItemDto> UpdateAsync(FaqItemDto dto)
        {
            var f = await _db.FaqItems.FindAsync(dto.FaqId);
            f.Category = dto.Category;
            f.QuestionVariant = dto.QuestionVariant;
            f.Answer = dto.Answer;
            f.SortOrder = dto.SortOrder;
            await _db.SaveChangesAsync();
            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            var f = await _db.FaqItems.FindAsync(id);
            _db.FaqItems.Remove(f);
            await _db.SaveChangesAsync();
        }

        public async Task<List<FaqItemDto>> SearchAsync(string q) =>
            await _db.FaqItems
              .Where(f => f.QuestionVariant.Contains(q))
              .OrderBy(f => f.SortOrder)
              .Select(f => new FaqItemDto
              {
                  FaqId = f.FaqId,
                  Category = f.Category,
                  QuestionVariant = f.QuestionVariant,
                  Answer = f.Answer,
                  SortOrder = f.SortOrder
              }).ToListAsync();
    }

}
