using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShareLoader.Data;
using ShareLoader.Downloader;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class AccountHelper
    {
        private Dictionary<int, AccountProfile> _profiles = new Dictionary<int, AccountProfile>();

        private DownloadContext _context;
        

        public AccountHelper(IConfiguration configuration)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();
            //optionsBuilder.UseMySql(configuration.GetConnectionString("MySQLConnection"));
            optionsBuilder.UseSqlite("Data Source=database.db");
            _context = new DownloadContext(optionsBuilder.Options);
        }

        public async Task<AccountProfile> GetProfile(AccountModel acc)
        {
            if (!_profiles.ContainsKey(acc.ID))
            {
                if (!_context.Accounts.Any(a => a.ID == acc.ID))
                    return null;
                AccountProfile profile = new AccountProfile(acc);
                IDownloader downloader = DownloadHelper.GetDownloader(profile);
                if (downloader == null)
                    return null;
                profile = await downloader.DoLogin(profile);
                _profiles.Add(acc.ID, profile);
            }

            return _profiles[acc.ID];
        }


        public async Task<AccountProfile> GetFreeProfile(DownloadItem item)
        {
            List<AccountModel> profiles = _context.Accounts.ToList();

            foreach(AccountModel acc in profiles)
            {
                if (_profiles.ContainsKey(acc.ID))
                    continue;
                AccountProfile p = new AccountProfile(acc);
                IDownloader downloader = DownloadHelper.GetDownloader(p);
                if (downloader == null)
                    continue;
                p = await downloader.DoLogin(p);
                _profiles.Add(acc.ID, p);
            }

            AccountProfile profile = null;

            try
            {
                profile = _profiles.First(kv => kv.Value.Model.Hoster == item.Hoster && kv.Value.Model.IsPremium == true && kv.Value.Model.TrafficLeft > 1 && kv.Value.Model.TrafficLeftWeek > 1).Value;
            }
            catch { }

            return profile;
        }
    }
}
