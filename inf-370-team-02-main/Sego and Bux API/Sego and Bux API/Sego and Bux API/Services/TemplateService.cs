using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public TemplateService(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<Template> CreateAsync(CreateTemplateDto dto)
        {
            var uploads = Path.Combine(_env.WebRootPath, "templates");
            Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
            var path = Path.Combine(uploads, fileName);
            await using var fs = new FileStream(path, FileMode.Create);
            await dto.File.CopyToAsync(fs);

            // 1) Create template record
            var tpl = new Template
            {
                Name = dto.Name,
                FilePath = "/templates/" + fileName
            };
            _db.Templates.Add(tpl);
            await _db.SaveChangesAsync();

            // 2) Link to product
            _db.ProductTemplates.Add(new ProductTemplate
            {
                ProductID = dto.ProductID,
                TemplateID = tpl.TemplateID
            });
            await _db.SaveChangesAsync();

            return tpl;
        }

        public async Task<IEnumerable<Template>> GetAllAsync()
            => await _db.Templates.ToListAsync();

        public async Task<Template?> GetByIdAsync(int id)
            => await _db.Templates.FindAsync(id);

        public async Task<IEnumerable<Template>> GetByProductAsync(int productId)
        {
            return await _db.ProductTemplates
                .Where(pt => pt.ProductID == productId)
                .Select(pt => pt.Template)
                .ToListAsync();
        }

        public async Task<Template?> UpdateAsync(int id, UpdateTemplateDto dto)
        {
            var tpl = await _db.Templates.FindAsync(id);
            if (tpl == null) return null;

            tpl.Name = dto.Name;
            if (dto.File != null)
            {
                var uploads = Path.Combine(_env.WebRootPath, "templates");
                var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
                var path = Path.Combine(uploads, fileName);
                await using var fs = new FileStream(path, FileMode.Create);
                await dto.File.CopyToAsync(fs);
                tpl.FilePath = "/templates/" + fileName;
            }
            await _db.SaveChangesAsync();

            // update join table:
            var link = await _db.ProductTemplates
                                .FindAsync(dto.ProductID, id);
            if (link == null)
            {
                _db.ProductTemplates.Add(new ProductTemplate
                {
                    ProductID = dto.ProductID,
                    TemplateID = tpl.TemplateID
                });
                await _db.SaveChangesAsync();
            }

            return tpl;
        }
        //hgfhgfg
        public async Task<bool> DeleteAsync(int id)
        {
            var tpl = await _db.Templates.FindAsync(id);
            if (tpl == null) return false;

            // remove join entries
            var links = _db.ProductTemplates
                           .Where(pt => pt.TemplateID == id);
            _db.ProductTemplates.RemoveRange(links);

            _db.Templates.Remove(tpl);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}