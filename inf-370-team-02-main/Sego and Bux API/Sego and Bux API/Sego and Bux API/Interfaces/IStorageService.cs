using System.IO;
using System.Threading.Tasks;

namespace Sego_and__Bux.Interfaces
{
    public interface IStorageService
    {
        string TempRoot { get; }
        Task<string> SaveTempAsync(Stream content, string fileName);
        Task<Stream> OpenReadAsync(string path);
        Task DeleteAsync(string path);
    }
}
