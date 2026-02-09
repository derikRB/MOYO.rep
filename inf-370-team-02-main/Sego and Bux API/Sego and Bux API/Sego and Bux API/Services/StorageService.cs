using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Services
{
    public class StorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        public StorageService(IWebHostEnvironment env) => _env = env;

        public string TempRoot
        {
            get
            {
                var path = Path.Combine(_env.ContentRootPath, "wwwroot", "temp");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        public async Task<string> SaveTempAsync(Stream content, string fileName)
        {
            var safeName = $"{Guid.NewGuid()}_{fileName}";
            var fullPath = Path.Combine(TempRoot, safeName);
            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(fs);
            return fullPath;
        }

        public Task<Stream> OpenReadAsync(string path)
        {
            Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(s);
        }

        public Task DeleteAsync(string path)
        {
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
