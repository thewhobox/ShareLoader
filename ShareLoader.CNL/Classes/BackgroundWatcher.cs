namespace ShareLoader.CNL.Classes;

using ShareLoader.Share;
using System.Diagnostics;

public class BackgroundWatcher : BackgroundService
{
    public static BackgroundWatcher Instance { get; set; }
    
    private FileSystemWatcher watcher = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Instance = this;        

        StartWatcher();

        while(!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100);
        }
    }

    public void StartWatcher()
    {
        if(watcher != null)
        {
            watcher.Changed -= OnChanged;
            watcher.Dispose();
        }

        string path = SettingsHelper.GetSetting("download");
        if(!System.IO.Directory.Exists(path))
        {
            System.Console.WriteLine("Der angegebene Ordner existiert nicht: " + path);
            System.Console.WriteLine("Watcher nicht gestartet");
            return;
        }
        System.Console.WriteLine("Starting watcher for Folder: " + path);
        watcher  = new FileSystemWatcher(path);
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Changed += OnChanged;
        watcher.Filter = "*.dlc";
        watcher.EnableRaisingEvents = true;
    }

    private static string lastFile;

    private static async void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (lastFile == e.Name || e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }
        lastFile = e.Name;
        string content = System.IO.File.ReadAllText(e.FullPath);
        CheckViewModel model = DecryptHelper.DecryptContainer(content);
        
        HttpClient client = new HttpClient();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(json);
        string linksb64 = System.Convert.ToBase64String(plainTextBytes);
        string host = SettingsHelper.GetSetting("host");

        ProcessStartInfo ps;
        try{
            var res = await client.GetStringAsync(host + "/Downloads/ApiAdd?links=" + linksb64);
            ps = new ProcessStartInfo(host + "/Downloads/Add")
            { 
                UseShellExecute = true, 
                Verb = "open" 
            };
        } catch{
            System.Console.WriteLine("Host ist nicht erreichbar!");
            ps = new ProcessStartInfo("http://localhost:9666/flash/error/1")
            { 
                UseShellExecute = true, 
                Verb = "open" 
            };
        }
        
        
        Process.Start(ps);
    }
}