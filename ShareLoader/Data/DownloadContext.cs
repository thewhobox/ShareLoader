using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Models;

namespace ShareLoader.Data;

public class DownloadContext : DbContext
{
    public DownloadContext() : base() { }
    public DownloadContext(DbContextOptions<DownloadContext> options) : base(options) { }

    public DbSet<DownloadGroup> Groups { get; set; }
    public DbSet<DownloadItem> Items { get; set; }
    //public DbSet<DownloadError> Errors { get; set; }
    public DbSet<AccountModel> Accounts { get; set; }
    //public DbSet<AppHash> Codes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite("Data Source=database.db");
    }
}

public class DownloadContextFactory : IDesignTimeDbContextFactory<DownloadContext>
{
    public DownloadContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();
        //optionsBuilder.UseMySql("server=mikegerst.de;userid=shareloader;password=Mein#pw#shareloader;database=shareloader;");
        //optionsBuilder.UseMySql($"server=mikegerst.de;userid=shareloader;password=Mein#pw#shareloader;database=shareloader;connectiontimeout=30");
        optionsBuilder.UseSqlite("Data Source=database.db");


        return new DownloadContext(optionsBuilder.Options);
    }
}