using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient; // Microsoft.Data.SqlClient
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sego_and__Bux.Interfaces;

namespace Sego_and__Bux.Services
{
    public class MaintenanceServiceSqlServer : IMaintenanceService
    {
        private readonly IConfiguration _cfg;
        private readonly IStorageService _storage;
        private readonly IHostApplicationLifetime _lifetime;

        // Neutral, non-OneDrive location you created and ACL’d:
        // - SQL service: Modify
        // - Your user (or app identity): Read
        private const string FallbackBackupDir = @"C:\SqlBackups\AppTemp";

        public MaintenanceServiceSqlServer(
            IConfiguration cfg,
            IStorageService storage,
            IHostApplicationLifetime lifetime)
        {
            _cfg = cfg;
            _storage = storage;
            _lifetime = lifetime;
        }

        private (string masterConn, string dbName) GetConns()
        {
            var appConn = _cfg.GetConnectionString("DefaultConnection")
                         ?? throw new InvalidOperationException("Missing DefaultConnection.");
            var builder = new SqlConnectionStringBuilder(appConn);
            var dbName = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            return (builder.ConnectionString, dbName);
        }

        private static async Task<bool> IsAzureSqlAsync(SqlConnection openConn)
        {
            using var cmd = openConn.CreateCommand();
            cmd.CommandText = "SELECT CAST(SERVERPROPERTY('EngineEdition') AS INT)";
            var v = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            return v == 5; // Azure SQL Database
        }

        private static string EnsureDir(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// BACKUP to the neutral folder (FallbackBackupDir) where SQL service has write.
        /// Then stream the .bak from the API with FileStream (your user has Read).
        /// </summary>
        public async Task<(string fileName, string contentType, Stream stream)> CreateBackupAsync()
        {
            var (masterConn, dbName) = GetConns();

            await using var conn = new SqlConnection(masterConn);
            await conn.OpenAsync();

            if (await IsAzureSqlAsync(conn))
                throw new NotSupportedException(
                    "Azure SQL does not support BACKUP/RESTORE to disk. Use Export/Import (.bacpac).");

            var backupDir = EnsureDir(FallbackBackupDir); // always use the neutral dir you ACL’d
            var fileName = $"{dbName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
            var serverFullPath = Path.Combine(backupDir, fileName);

            // BACKUP executes inside SQL (service account writes the file)
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
BACKUP DATABASE [" + dbName + @"]
TO DISK = @p0
WITH INIT, COPY_ONLY, STATS = 5;";
                cmd.Parameters.Add(new SqlParameter("@p0", SqlDbType.NVarChar) { Value = serverFullPath });
                await cmd.ExecuteNonQueryAsync();
            }

            // Stream the file from the API process (your identity has Read on this folder)
            var fs = new FileStream(serverFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return (fileName, "application/octet-stream", fs);
        }

        /// <summary>
        /// For restore, stage the uploaded .bak into the same neutral folder (FallbackBackupDir),
        /// then RESTORE FROM DISK using that path (so SQL service can read it).
        /// </summary>
        public async Task ScheduleRestoreAsync(IFormFile backupFile)
        {
            if (backupFile == null || backupFile.Length == 0)
                throw new InvalidOperationException("Empty file.");

            if (!backupFile.FileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only .bak files are supported for restore.");

            // Stage into the neutral folder (not OneDrive)
            var stagingDir = EnsureDir(FallbackBackupDir);
            var stagedPath = Path.Combine(
                stagingDir,
                $"{Path.GetFileNameWithoutExtension(backupFile.FileName)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak"
            );

            // Copy upload into the neutral folder
            await using (var src = backupFile.OpenReadStream())
            await using (var dst = new FileStream(stagedPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await src.CopyToAsync(dst);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var (masterConn, dbName) = GetConns();
                    await using var conn = new SqlConnection(masterConn);
                    await conn.OpenAsync();

                    if (await IsAzureSqlAsync(conn))
                        throw new NotSupportedException(
                            "Azure SQL does not support RESTORE DATABASE from .bak. Use Import (.bacpac).");

                    // Perform restore from the staged path (SQL service has read rights here)
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = $@"
ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{dbName}] FROM DISK = @p0 WITH REPLACE;
ALTER DATABASE [{dbName}] SET MULTI_USER;";
                    cmd.Parameters.Add(new SqlParameter("@p0", SqlDbType.NVarChar) { Value = stagedPath });

                    await cmd.ExecuteNonQueryAsync();
                }
                finally
                {
                    try { System.IO.File.Delete(stagedPath); } catch { /* ignore */ }
                    await Task.Delay(1500);
                    _lifetime.StopApplication();
                }
            });
        }
    }
}
