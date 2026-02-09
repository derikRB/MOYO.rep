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
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        public CategoryService(ApplicationDbContext context) => _context = context;

        public async Task<Category> AddCategoryAsync(CategoryDto dto)
        {
            var category = new Category { CategoryName = dto.CategoryName, Description = dto.Description };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return false;
            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync() => await _context.Categories.ToListAsync();

        public async Task<Category?> GetCategoryByIdAsync(int id) => await _context.Categories.FindAsync(id);

        public async Task<Category> UpdateCategoryAsync(int id, CategoryDto dto)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return null!;
            cat.CategoryName = dto.CategoryName;
            cat.Description = dto.Description;
            await _context.SaveChangesAsync();
            return cat;
        }
    }
}
