using ShareLoader.Models;

namespace ShareLoader.Background;

public class BackgroundTasks : BackgroundService
{
    public static BackgroundTasks Instance { get; set; }

    AccountChecker account;
    DownloadChecker download;
    ExtractChecker extract;
    MoveChecker move;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Instance = this;
        System.Console.WriteLine("Starte Backgrounder");
        account = new AccountChecker();
        download = new DownloadChecker(account);
        extract = new ExtractChecker(download);
        move = new MoveChecker(download, extract);
        await Check(stoppingToken);
    }

    public async Task StopDownload(DownloadItem item)
    {
        await download.StopDownload(item);
    }

    private async Task Check(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            if(account.LastChecked + TimeSpan.FromMinutes(10) < DateTime.Now)
            {
                Console.WriteLine("AccountChecker neu gestartet");
                account = new AccountChecker();
            }
            if(download.LastChecked + TimeSpan.FromMinutes(10) < DateTime.Now)
            {
                Console.WriteLine("DownloadChecker neu gestartet");
                download = new DownloadChecker(account);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        
        Console.WriteLine("BackgroundTask beendet");
    }

}