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
    private List<DownloadModel> _currentItems = new List<DownloadModel>();
    AccountChecker account;
    Timer _timer;
    int checkCounter = 0;

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


    bool flag1 = false;
    private void CheckTimer(object? state)
    {
        if(SettingsHelper.GetSetting<SettingsModel>("settings") == null)
        {
            if(!flag1) System.Console.WriteLine("Es wurden noch keine Einstellungen gespeichert");
            flag1 = true;
            return;
        }
        flag1 = false;
        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        foreach(DownloadModel model in _currentItems.ToList())
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

    bool flag2 = false;
    private async void Check()
    {
        using(Data.DownloadContext context = new Data.DownloadContext())
        {
            foreach(DownloadItem item in context.Items.Where(i => i.State == States.Downloading || i.State == States.Extracting || i.State == States.Moving).ToList())
            {
                item.State = States.Error;
                context.Items.Update(item);
                context.Errors.Add(new ErrorModel() { GroupId = item.DownloadGroupID, ItemId = item.Id, Text = "Startup: Datei hatte den Status Downloading", FileName = item.Name });
            }
            context.SaveChanges();
        }

        while(true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            SettingsModel settings = SettingsHelper.GetSetting<SettingsModel>("settings");
            if(settings == null || _currentItems.Count >= settings.MaxDownloads) continue;


            if(!System.IO.Directory.Exists(settings.DownloadFolder))
            {
                if(!flag2) System.Console.WriteLine("Downloadfolder does not exist: " + settings.DownloadFolder);
                flag2 = true;
                continue;
            }
            flag2 = false;

            List<DownloadItem> items = new List<DownloadItem>();
            using(DownloadContext context = new DownloadContext())
            {
                foreach(DownloadGroup group in context.Groups.Where(g => g.State == GroupStates.Normal))
                {
                    items.AddRange(context.Items.Where(i => i.DownloadGroupID == group.Id && i.State == States.Waiting).ToList());   
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
                await manager.DoLogin(profile);
                _currentItems.Add(model);
                checkCounter = 0;
                DoDownload(model);
                break;
            }

            checkCounter++;
            if(checkCounter > 2) nothingToDownload = true;
            LastChecked = DateTime.Now;
        }
    }

    private async void DoDownload(DownloadModel model)
    {
        ChangeItemState(model.Item, States.Downloading);
        System.Console.WriteLine($"Downloading now: {model.Item.ItemId} ({model.Item.Name}) with hoster {model.Manager.Identifier}");
        bool acceptRange = await model.Manager.CheckStreamRange(model.Item, model.Profile);
        

        SettingsModel? settings = SettingsHelper.GetSetting<SettingsModel>("settings");
        if(settings == null)
        {
            ChangeItemState(model.Item, States.Waiting);
            Console.WriteLine("Aborted Download due to no settings found");
            return;
        }

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
        Stream? s = null;

        if(!acceptRange || !System.IO.File.Exists(filePath))
        {
            s = await model.Manager.GetDownloadStream(model.Item, model.Profile);
        } else {
            FileInfo info = new FileInfo(filePath);
            if(info.Length == model.Item.Size)
            {
                ChangeItemState(model.Item, States.Downloaded);
                _ = SocketHelper.Instance.SendIDDownloaded(model.Item);
                _currentItems.Remove(model);
                System.Diagnostics.Debug.WriteLine("Datei schon komplett heruntergeladen");
                return;
            }
            if(info.Length > model.Item.Size)
            {
                ChangeItemState(model.Item, States.Error);
                _ = SocketHelper.Instance.SendIDError(model.Item, "Datei ist größer als möglich");
                _currentItems.Remove(model);
                System.Diagnostics.Debug.WriteLine("Datei ist größer als möglich");
                return;
            }
            s = await model.Manager.GetDownloadStream(model.Item, model.Profile, info.Length);
        }

        if(s == null)
        {
            ChangeItemState(model.Item, States.Error);
            _ = SocketHelper.Instance.SendIDError(model.Item, "Stream konnte nicht geladen werden");
            _currentItems.Remove(model);
            return;
        }

        System.Console.WriteLine($"Downloading now into: {filePath}");

        System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate);

        try{
            fs.Position = fs.Length;
            await s.CopyToAsync(fs, model.Token.Token);
            ChangeItemState(model.Item, States.Downloaded);
            _ = SocketHelper.Instance.SendIDDownloaded(model.Item);
        } catch (Exception ex) {
            ChangeItemState(model.Item, States.Error, ex.Message);
            _ = SocketHelper.Instance.SendIDError(model.Item, ex.Message);
        }
        fs.Close();
        fs.Dispose();
        s.Close();
        s.Dispose();

        _currentItems.Remove(model);
    }

    private void ChangeItemState(DownloadItem item, States state, string message = "")
    {
        if(item.State == state) return;
        item.State = state;
        using(DownloadContext context = new DownloadContext())
        {
            context.Items.Update(item);
            if(state == States.Error && item != null)
            {
                context.Errors.Add(new ErrorModel() { GroupId = item.DownloadGroupID, ItemId = item.Id, Text = message, FileName = item.Name });
            }
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