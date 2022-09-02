using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using ShareLoader.Models;
using ShareLoader.Share;
using System.Linq;

namespace ShareLoader.Background;

public class DownloadChecker
{

    public DateTime LastChecked { get; set; } = DateTime.Now;
    public bool isDownloading = false;
    public bool nothingToDownload = false;
    private CancellationTokenSource? _downloadToken;
    private DownloadGroup _currentGroup;
    private DownloadItem? _currentItem;
    AccountChecker account;
    Timer _timer;

    public DownloadChecker(AccountChecker acc)
    {
        account = acc;
        Check();
        _timer = new Timer(CheckTimer, null, 0, 1000);
    }

    public async Task StopDownload(DownloadItem item)
    {
        if(_currentItem == null || _currentItem.Id != item.Id || _downloadToken == null) return;
        _downloadToken.Cancel();

        while(_currentItem != null)
            await Task.Delay(5);
    }

    private void CheckTimer(object? state)
    {
        if(_currentItem == null) return;

        double _current = 0;
        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        string filePath = Path.Combine(downloadPath, _currentItem.DownloadGroupID.ToString(), "files", _currentItem.Name);

        if (File.Exists(filePath)){
            FileInfo info = new FileInfo(filePath);
            _current = info.Length;
        }

        if(_currentItem == null) return;
        double percent = Math.Floor((_current / _currentItem.Size) * 100);
        _ = SocketHelper.Instance.SendIDPercentage(_currentItem, 15000, percent);
    }

    private async void Check()
    {
        while(true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if(isDownloading) continue;

            IEnumerable<DownloadItem> items;
            using(DownloadContext context = new DownloadContext())
            {
                items = context.Items.Where(i => i.State == States.Waiting).ToList();
            }
            foreach(DownloadItem item in items)
            {
                IDownloadManager manager = DownloadHelper.GetDownloader(item.Url);
                AccountProfile profile = account.GetProfile(item);
                if(profile == null) continue;
                nothingToDownload = false;
                DoDownload(item, manager, profile);
                break;
            }
            nothingToDownload = true;
            LastChecked = DateTime.Now;
        }
    }

    private async void DoDownload(DownloadItem item, IDownloadManager manager, AccountProfile profile)
    {
        isDownloading = true;
        _downloadToken = new CancellationTokenSource();
        _currentItem = item;

        ChangeItemState(item, States.Downloading);
        System.Console.WriteLine($"Downloading now: {item.ItemId} ({item.Name}) with hoster {manager.Identifier}");
        Stream s = await manager.GetDownloadStream(item, profile);

        SettingsModel settings = SettingsHelper.GetSetting<SettingsModel>("settings");

        string groupDir = System.IO.Path.Combine(settings.DownloadFolder, item.DownloadGroupID.ToString());
        string filesDir = System.IO.Path.Combine(settings.DownloadFolder, item.DownloadGroupID.ToString(), "files");
        string extractDir = System.IO.Path.Combine(settings.DownloadFolder, item.DownloadGroupID.ToString(), "extracted");

        if (!System.IO.Directory.Exists(groupDir))
            System.IO.Directory.CreateDirectory(groupDir);

        if (!System.IO.Directory.Exists(filesDir))
            System.IO.Directory.CreateDirectory(filesDir);

        if (!System.IO.Directory.Exists(extractDir))
            System.IO.Directory.CreateDirectory(extractDir);

        string filePath = System.IO.Path.Combine(filesDir, item.Name);
        
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        System.Console.WriteLine($"Downloading now into: {filePath}");

        System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate);

        try{
            await s.CopyToAsync(fs, _downloadToken.Token);
            ChangeItemState(item, States.Downloaded);
            _ = SocketHelper.Instance.SendIDDownloaded(_currentItem);
        } catch {
            ChangeItemState(item, States.Error);
            _ = SocketHelper.Instance.SendIDError(_currentItem);
        }
        fs.Close();
        fs.Dispose();
        s.Close();
        s.Dispose();

        _currentItem = null;
        _downloadToken = null;
        isDownloading = false;
    }

    private void ChangeItemState(DownloadItem item, States state)
    {
        if(item.State == state) return;
        item.State = state;
        using(DownloadContext context = new DownloadContext())
        {
            context.Items.Update(item);
            context.SaveChanges();
        }
    }
}