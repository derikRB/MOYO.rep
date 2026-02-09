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
    public class ProductTypeService : IProductTypeService
    {
        private readonly ApplicationDbContext _context;
        public ProductTypeService(ApplicationDbContext context) => _context = context;

        public async Task<ProductType> AddProductTypeAsync(ProductTypeDto dto)
        {
            var pt = new ProductType { ProductTypeName = dto.ProductTypeName, Description = dto.Description, CategoryID = dto.CategoryID };
            _context.ProductTypes.Add(pt);
            await _context.SaveChangesAsync();
            return pt;
        }

        public async Task<bool> DeleteProductTypeAsync(int id)
        {
            var pt = await _context.ProductTypes.FindAsync(id);
            if (pt == null) return false;
            _context.ProductTypes.Remove(pt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ProductType>> GetAllProductTypesAsync() =>
            await _context.ProductTypes.Include(pt => pt.Category).ToListAsync();

        public async Task<ProductType?> GetProductTypeByIdAsync(int id) =>
            await _context.ProductTypes.Include(pt => pt.Category).FirstOrDefaultAsync(pt => pt.ProductTypeID == id);

        public async Task<ProductType> UpdateProductTypeAsync(int id, ProductTypeDto dto)
        {
            var pt = await _context.ProductTypes.FindAsync(id);
            if (pt == null) return null!;
            pt.ProductTypeName = dto.ProductTypeName;
            pt.Description = dto.Description;
            pt.CategoryID = dto.CategoryID;
            await _context.SaveChangesAsync();
            return pt;
        }
    }

}
