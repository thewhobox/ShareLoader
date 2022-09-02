using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using ShareLoader.Models;
using System.Linq;

namespace ShareLoader.Background;

public class AccountChecker
{
    public DateTime LastChecked { get; set; } = DateTime.Now;

    Dictionary<int, AccountProfile> Profiles = new Dictionary<int, AccountProfile>();


    public AccountChecker()
    {
        Check();
    }

    private async void Check()
    {
        using (DownloadContext context = new DownloadContext())
        {
            foreach (AccountModel acc in context.Accounts)
                Profiles.Add(acc.Id, new AccountProfile(acc));
        }

        while (true)
        {
            using (DownloadContext context = new DownloadContext())
            {
                foreach (AccountModel acc in context.Accounts.ToList())
                {
                    if (Profiles.ContainsKey(acc.Id)) continue;
                    Profiles.Add(acc.Id, new AccountProfile(acc));
                }
            }

            using (DownloadContext context = new DownloadContext())
            {
                foreach (AccountProfile profile in Profiles.Values)
                {
                    IDownloadManager downloader = DownloadHelper.GetDownloader(profile);

                    if (!profile.IsLoggedIn)
                        profile.IsLoggedIn = await downloader.DoLogin(profile);

                    if (!profile.IsLoggedIn) continue;
                    await downloader.GetAccounInfo(profile);

                    context.Accounts.Update(profile.Model);
                }

                context.SaveChanges();
            }
            
            LastChecked = DateTime.Now;
            await Task.Delay(TimeSpan.FromMinutes(1));
        }

    }

    public AccountProfile GetProfile(DownloadItem item)
    {
        //TODO also check if enough traffic left
        AccountProfile profile = Profiles.Values.SingleOrDefault(p => p.Model.Hoster == item.Hoster);
        return profile;
    }

    private async Task Login(AccountModel acc)
    {
        AccountProfile profile = new AccountProfile(acc);

        IDownloadManager downloader = DownloadHelper.GetDownloader(profile);
        bool success = await downloader.DoLogin(profile);
        if (success)
        {
            Console.WriteLine($"Erfolgreich angemeldet bei {downloader.Identifier} mit {acc.Username}");
            Profiles.Add(acc.Id, profile);
        }
        else
            Console.WriteLine($"Anmeldung fehlgeschlagen bei {downloader.Identifier} mit {acc.Username}");
    }
}