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
    public bool isDownloading {
        get { return _currentItems.Count > 0; }
    }
    public bool nothingToDownload = false;
    private DownloadGroup _currentGroup;
    private List<DownloadModel> _currentItems = new List<DownloadModel>();
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
        if(_currentItems.Count == 0) return;

        DownloadModel? model = _currentItems.SingleOrDefault(i => i.Item.Id == item.Id);
        if(model == null) return;

        model.Token.Cancel();

        while(_currentItems.Any(i => i.Item.Id == item.Id))
            await Task.Delay(5);
    }

    private void CheckTimer(object? state)
    {
        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        foreach(DownloadModel model in _currentItems)
        {
            double _current = 0;
            string filePath = Path.Combine(downloadPath, model.Item.DownloadGroupID.ToString(), "files", model.Item.Name);

            if (File.Exists(filePath)){
                FileInfo info = new FileInfo(filePath);
                _current = info.Length;
            }

            double percent = Math.Floor((_current / model.Item.Size) * 100);
            _ = SocketHelper.Instance.SendIDPercentage(model.Item, 15000, percent);
        }
    }

    private async void Check()
    {
        while(true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            SettingsModel settings = SettingsHelper.GetSetting<SettingsModel>("settings");
            if(_currentItems.Count >= settings.MaxDownloads) continue;

            List<DownloadItem> items = new List<DownloadItem>();
            using(DownloadContext context = new DownloadContext())
            {
                foreach(DownloadGroup group in context.Groups.Where(g => g.State == GroupStates.Normal))
                {
                    items.AddRange(context.Items.Where(i => i.State == States.Waiting).ToList());   
                }
            }
            foreach(DownloadItem item in items)
            {
                IDownloadManager manager = DownloadHelper.GetDownloader(item.Url);
                AccountProfile? profile = account.GetProfile(item);
                if(profile == null) continue;
                nothingToDownload = false;
                DownloadModel model = new DownloadModel() {
                    Item = item, 
                    Manager = manager, 
                    Profile = profile
                };
                _currentItems.Add(model);
                DoDownload(model);
                break;
            }
            nothingToDownload = true;
            LastChecked = DateTime.Now;
        }
    }

    private async void DoDownload(DownloadModel model)
    {
        ChangeItemState(model.Item, States.Downloading);
        System.Console.WriteLine($"Downloading now: {model.Item.ItemId} ({model.Item.Name}) with hoster {model.Manager.Identifier}");
        Stream s = await model.Manager.GetDownloadStream(model.Item, model.Profile);
        if(s == null)
        {
            ChangeItemState(model.Item, States.Error);
            _ = SocketHelper.Instance.SendIDError(model.Item);
            _currentItems.Remove(model);
            return;
        }

        SettingsModel settings = SettingsHelper.GetSetting<SettingsModel>("settings");

        string groupDir = System.IO.Path.Combine(settings.DownloadFolder, model.Item.DownloadGroupID.ToString());
        string filesDir = System.IO.Path.Combine(settings.DownloadFolder, model.Item.DownloadGroupID.ToString(), "files");
        string extractDir = System.IO.Path.Combine(settings.DownloadFolder, model.Item.DownloadGroupID.ToString(), "extracted");

        if (!System.IO.Directory.Exists(groupDir))
            System.IO.Directory.CreateDirectory(groupDir);

        if (!System.IO.Directory.Exists(filesDir))
            System.IO.Directory.CreateDirectory(filesDir);

        if (!System.IO.Directory.Exists(extractDir))
            System.IO.Directory.CreateDirectory(extractDir);

        string filePath = System.IO.Path.Combine(filesDir, model.Item.Name);
        
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        System.Console.WriteLine($"Downloading now into: {filePath}");

        System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate);

        try{
            await s.CopyToAsync(fs, model.Token.Token);
            ChangeItemState(model.Item, States.Downloaded);
            _ = SocketHelper.Instance.SendIDDownloaded(model.Item);
        } catch {
            ChangeItemState(model.Item, States.Error);
            _ = SocketHelper.Instance.SendIDError(model.Item);
        }
        fs.Close();
        fs.Dispose();
        s.Close();
        s.Dispose();

        _currentItems.Remove(model);
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

public class DownloadModel
{
    public DownloadItem Item { get; set; }
    public IDownloadManager Manager { get; set; }
    public AccountProfile Profile { get; set; }
    public CancellationTokenSource Token { get; set; } = new CancellationTokenSource();
}