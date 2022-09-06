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
        System.Console.WriteLine("Starte BackgroundTasker");


        using(Data.DownloadContext context = new Data.DownloadContext())
        {
            foreach(DownloadItem item in context.Items.Where(i => i.State == States.Downloading || i.State == States.Extracting || i.State == States.Moving).ToList())
            {
                item.State = States.Error;
                context.Items.Update(item);
            }
            context.SaveChanges();
        }

        account = new AccountChecker();
        download = new DownloadChecker(account);
        extract = new ExtractChecker(download);
        move = new MoveChecker(download, extract);
        await Check(stoppingToken);
    }

    public async Task StopExtract(DownloadItem item)
    {
        await extract.StopExtract(item);
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
            if(extract.LastChecked + TimeSpan.FromMinutes(10) < DateTime.Now)
            {
                Console.WriteLine("ExtractChecker neu gestartet");
                extract = new ExtractChecker(download);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        
        Console.WriteLine("BackgroundTask beendet");
    }

}