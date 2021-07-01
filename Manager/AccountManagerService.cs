using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ShareLoader.Classes;
using ShareLoader.Classes.Exceptions;
using ShareLoader.Data;
using ShareLoader.Downloader;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShareLoader.Manager
{
    public class AccountManagerService : BackgroundService
    {
        public readonly DownloadContext _context;
        public readonly AccountHelper _account;
        public readonly DownloadSettings _settings;

        public AccountManagerService(IConfiguration configuration, AccountHelper account, DownloadSettings settings)
        {
            Console.WriteLine("Erstelle AccountManager");
            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();
            string datapath = Path.Combine(settings.DownloadFolder, "database.db");
            optionsBuilder.UseSqlite("Data Source=" + datapath);

            _context = new DownloadContext(optionsBuilder.Options);
            _context.Database.Migrate();

            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(settings.LogFolder, "test.txt"), "Huhu, hier bin ich");
            } catch(Exception ex)
            {
                Console.WriteLine("Fehler beim erstellen der Datei!");
                Console.WriteLine(ex.Message);
            }

            _account = account;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                List<AccountModel> accs = _context.Accounts.ToList();
                foreach(AccountModel acc in accs)
                {
                    await CheckAccount(acc);
                }

                await Task.Delay(TimeSpan.FromSeconds(20)); //FromMinutes(1));
            }
        }

        public async Task CheckAccount(AccountModel account)
        {
            //Console.WriteLine("Checke Account " + account.Name);
            AccountProfile profile = await _account.GetProfile(account);            
            IDownloader downloader = DownloadHelper.GetDownloader(profile);
            if (downloader == null)
            {
                Console.WriteLine("No downloader for profile");
                return;
            }

            AccountModel model = null;
            try
            {
                model = await downloader.GetAccountInfo(profile);
            } catch(Exception ex)
            {
                Console.WriteLine("Fehler beim holen der Accountinfos");
                Console.WriteLine(ex.Message);
            }

            if (model == null)
                return;
            
            _context.Accounts.Update(model);
            _context.SaveChanges();

            try
            {
                _context.SaveChanges();
            }
            catch
            {

            }
            await SocketHandler.Instance.SendAITrafficDay(model);

        }
    }
}
