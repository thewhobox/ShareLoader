

namespace ShareLoader.Background;

public class BackgroundTasks : BackgroundService
{
    AccountChecker account;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        System.Console.WriteLine("Starte Backgrounder");
        account = new AccountChecker();
        await Check(stoppingToken);
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

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        
        Console.WriteLine("BackgroundTask beendet");
    }

}