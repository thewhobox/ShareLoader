using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Data
{
    public class DownloadContext : DbContext
    {
        public DownloadContext(DbContextOptions<DownloadContext> options)
            : base(options)
        { }

        public DbSet<DownloadGroup> Groups { get; set; }
        public DbSet<DownloadItem> Items { get; set; }
        public DbSet<DownloadError> Errors { get; set; }
        public DbSet<AccountModel> Accounts { get; set; }
        public DbSet<StatisticModel> Statistics { get; set; }
        public DbSet<AppHash> Codes { get; set; }
    }

    public class DownloadContextFactory : IDesignTimeDbContextFactory<DownloadContext>
    {
        public DownloadContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();
            //optionsBuilder.UseMySql("server=mikegerst.de;userid=shareloader;password=Mein#pw#shareloader;database=shareloader;");
            optionsBuilder.UseMySql($"server=mikegerst.de;userid=shareloader;password=Mein#pw#shareloader;database=shareloader;connectiontimeout=30");

            return new DownloadContext(optionsBuilder.Options);
        }
    }
}
