using System;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;

namespace SegoAndBux.Tests.Common
{
    public static class TestDb
    {
        public static ApplicationDbContext NewContext(string? dbName = null)
        {
            dbName ??= Guid.NewGuid().ToString("N");
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
