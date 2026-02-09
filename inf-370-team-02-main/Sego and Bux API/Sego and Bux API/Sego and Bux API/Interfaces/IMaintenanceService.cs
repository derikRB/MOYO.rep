using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sego_and__Bux.Interfaces
{
    public interface IMaintenanceService
    {
        Task<(string fileName, string contentType, Stream stream)> CreateBackupAsync();
        Task ScheduleRestoreAsync(IFormFile backupFile);
    }
}