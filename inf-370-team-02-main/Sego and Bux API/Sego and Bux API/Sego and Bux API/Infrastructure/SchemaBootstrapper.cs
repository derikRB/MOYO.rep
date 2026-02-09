using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class SchemaBootstrapper : IHostedService
    {
        private readonly IConfiguration _cfg;
        public SchemaBootstrapper(IConfiguration cfg) => _cfg = cfg;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var cs = _cfg.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync(cancellationToken);

            // SystemConfig (key/value)
            await ExecAsync(conn, @"
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'SystemConfigs' AND type = 'U')
BEGIN
  CREATE TABLE SystemConfigs(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL UNIQUE,
    [Value] NVARCHAR(4000) NOT NULL
  );
END
");

            // AuditLog
            await ExecAsync(conn, @"
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'AuditLogs' AND type = 'U')
BEGIN
  CREATE TABLE AuditLogs(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UtcTimestamp DATETIME2 NOT NULL,
    UserId NVARCHAR(64) NULL,
    UserEmailSnapshot NVARCHAR(256) NULL,
    Action NVARCHAR(32) NOT NULL,
    Entity NVARCHAR(64) NOT NULL,
    EntityId NVARCHAR(64) NULL,
    BeforeJson NVARCHAR(MAX) NULL,
    AfterJson NVARCHAR(MAX) NULL,
    Ip NVARCHAR(64) NULL,
    UserAgent NVARCHAR(512) NULL
  );
  CREATE INDEX IX_AuditLogs_UtcTimestamp ON AuditLogs(UtcTimestamp);
  CREATE INDEX IX_AuditLogs_UserEmailSnapshot ON AuditLogs(UserEmailSnapshot);
  CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
  CREATE INDEX IX_AuditLogs_Entity ON AuditLogs(Entity);
END
");

            // OrderLine shadow fields
            await ExecAsync(conn, @"
IF COL_LENGTH('OrderLines','UnitPriceAtSale') IS NULL
  ALTER TABLE OrderLines ADD UnitPriceAtSale DECIMAL(18,2) NULL;

IF COL_LENGTH('OrderLines','VatRateAtSale') IS NULL
  ALTER TABLE OrderLines ADD VatRateAtSale DECIMAL(5,2) NULL;

IF COL_LENGTH('OrderLines','ProductNameSnapshot') IS NULL
  ALTER TABLE OrderLines ADD ProductNameSnapshot NVARCHAR(200) NULL;

IF COL_LENGTH('OrderLines','SkuSnapshot') IS NULL
  ALTER TABLE OrderLines ADD SkuSnapshot NVARCHAR(64) NULL;

IF COL_LENGTH('OrderLines','TemplateVersion') IS NULL
  ALTER TABLE OrderLines ADD TemplateVersion NVARCHAR(50) NULL;

IF COL_LENGTH('OrderLines','CustomizationJsonPath') IS NULL
  ALTER TABLE OrderLines ADD CustomizationJsonPath NVARCHAR(400) NULL;
");

            // Soft delete shadow columns
            await ExecAsync(conn, @"
IF COL_LENGTH('Products','IsDeleted') IS NULL
  ALTER TABLE Products ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT(0);
IF COL_LENGTH('Categories','IsDeleted') IS NULL
  ALTER TABLE Categories ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Categories_IsDeleted DEFAULT(0);
IF COL_LENGTH('ProductTypes','IsDeleted') IS NULL
  ALTER TABLE ProductTypes ADD IsDeleted BIT NOT NULL CONSTRAINT DF_ProductTypes_IsDeleted DEFAULT(0);
");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task ExecAsync(SqlConnection conn, string sql)
        {
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
