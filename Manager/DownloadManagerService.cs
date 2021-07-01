using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Downloader;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ShareLoader.Manager
{
    public class DownloadManagerService : BackgroundService
    {
        private DownloadSettings _settings;
        private readonly DownloadContext _context;
        private readonly AccountHelper _account;
        public static bool _isDownloading = false;
        public static bool _stopRequested = false;

        private Timer _state;
        private double _current = 0;
        private double _lastCurrent = 0;
        private bool _isChecking = false;
        private List<double> _lastReadList;
        StreamWriter w;
        string logday = "";

        private List<object> queryUpdate = new List<object>();
        private List<object> queryAdd = new List<object>();
        private string logfile = "";

        public void Log(string log)
        {
            Console.WriteLine("DownloadManager " + log);
            bool logDateExists = DateTime.Now.ToShortDateString().Replace("/", ".") == logday;
            if (!logDateExists)
            {
                if (!Directory.Exists(_settings.LogFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(_settings.LogFolder);
                    }
                    catch
                    {
                        Console.WriteLine("Could not create log Folder at: " + _settings.LogFolder);
                        return;
                    }
                }
                logday = DateTime.Now.ToShortDateString().Replace("/", ".");
                logfile = Path.Combine(_settings.LogFolder, "log-" + logday + "-down.log");

                if (!File.Exists(logfile))
                {
                    try
                    {
                        w = new StreamWriter(File.Create(logfile));
                    }
                    catch
                    {
                        Console.WriteLine("Could not create log File at: " + logfile);
                        return;
                    }
                }
            }

            if (w == null) return;

            w.WriteLine(DateTime.Now.ToShortTimeString() + " - " + log);
            w.Flush();
        }

        public async void CheckQuery(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000);

                if (queryUpdate.Count == 0 && queryAdd.Count == 0)
                    continue;

                object[] temp;

                if (queryUpdate.Count > 0)
                {
                    temp = new object[queryUpdate.Count + 10];
                    queryUpdate.CopyTo(temp);
                    queryUpdate.Clear();

                    foreach (object obj in temp)
                        if (obj != null)
                            _context.Update(obj);
                }

                if (queryAdd.Count > 0)
                {
                    temp = new object[queryAdd.Count + 10];
                    queryAdd.CopyTo(temp);
                    queryAdd.Clear();

                    foreach (object obj in temp)
                        if (obj != null)
                            _context.Add(obj);
                }

                int x = 0;
                while (true)
                {
                    try
                    {
                        _context.SaveChanges();
                        break;
                    }
                    catch
                    {
                    }

                    x++;
                    if (x >= 10)
                    {
                        Log("Query Too many tries");
                        break;
                    }
                    await Task.Delay(500);
                }
            }
        }

        public DownloadManagerService(IConfiguration configuration, AccountHelper account)
        {
            _settings = DownloadSettings.Load();


            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();
            //optionsBuilder.UseMySql("server=teamserver;userid=admin;password=Mein#pw#mysqladmin;database=shareloader;");
            //optionsBuilder.UseMySql(configuration.GetConnectionString("MySQLConnection"));
            optionsBuilder.UseSqlite("Data Source=database.db");


            _context = new DownloadContext(optionsBuilder.Options);
            _account = account;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CheckQuery(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalDownload), stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (_isDownloading)
                {
                    continue;
                }

                _isDownloading = true;
                List<DownloadGroup> groups = new List<DownloadGroup>();
                try
                {
                    _settings = DownloadSettings.Load();
                    groups = _context.Groups.Where(g => g.IsTemp == false).ToList();
                }
                catch (Exception e)
                {
                    Log("Load Error: " + e.Message);
                }

                bool flagDownload = false;

                foreach (DownloadGroup group in groups)
                {
                    if (!_context.Items.Any(i => i.DownloadGroupID == group.ID && i.State == DownloadItem.States.Waiting))
                    {
                        Log("No Items in " + group.Name);
                        continue;
                    }
                    Log("Checked Group found item: " + group.Name);

                    IEnumerable<DownloadItem> _items = _context.Items.Where(i => i.DownloadGroupID == group.ID && i.State == DownloadItem.States.Waiting).OrderBy(i => i.Name);

                    foreach (DownloadItem _item in _items)
                    {
                        IDownloader downloader = DownloadHelper.GetDownloader(_item);
                        if (downloader == null)
                        {
                            Log("Download Interface Error - " + _item.ID.ToString() + " - " + _item.Hoster);
                            _item.State = DownloadItem.States.Error;
                            queryUpdate.Add(_item);
                            queryAdd.Add(new DownloadError(1, _item, new Exception("No DownloadInterface for " + _item.Hoster)));
                            await SocketHandler.Instance.SendIDError(_item);
                            continue;
                        }

                        if (downloader.IsFree)
                        {
                            flagDownload = true;
                            StartDownload(group, _item, new AccountProfile(new AccountModel()));
                            break;
                        }
                        else
                        {
                            AccountProfile profile = await _account.GetFreeProfile(_item);
                            if (profile != null)
                            {
                                flagDownload = true;
                                StartDownload(group, _item, profile);
                                break;
                            }
                            else
                            {
                                Log("No Account for hoster: " + downloader.Identifier);
                                _item.State = DownloadItem.States.Error;

                                queryUpdate.Add(_item);
                                queryAdd.Add(new DownloadError(2, _item, new Exception("No Account for " + downloader.Identifier)));

                                await SocketHandler.Instance.SendIDError(_item);
                                continue;
                            }
                        }
                    }

                    if (flagDownload)
                        break;
                }

                if (!flagDownload)
                    _isDownloading = false;
            }
        }

        private async void CheckStatus(object state)
        {
            if (_isChecking) return;
            _isChecking = true;

            DownloadItem item = (DownloadItem)state;

            double temp = 0;
            double loaded = _current - _lastCurrent;
            _lastCurrent = _current;
            _lastReadList.Add(loaded);
            if (_lastReadList.Count > 10)
                _lastReadList.RemoveAt(0);

            foreach (double readed in _lastReadList)
                temp += readed;

            double averageRead = (temp / _lastReadList.Count) * 2;

            if (averageRead <= 0.1 || averageRead > 209715200)
            {
                _isChecking = false;
                return;
            }

            StatisticModel stat = new StatisticModel();
            stat.EntityID = item.ID;
            stat.EntityType = "ItemAverageDownload";
            stat.Value = averageRead / 1024 / 1024;
            stat.Source = StatisticModel.SourceType.Item;

            queryAdd.Add(stat);

            double percent = Math.Floor((_current / item.Size) * 100);
            await SocketHandler.Instance.SendIDPercentage(item, averageRead, percent.ToString());


            _isChecking = false;
        }

        private async Task StartDownload(DownloadGroup group, DownloadItem item, AccountProfile profile)
        {
            Log("State Downloading - " + item.ID.ToString() + " - " + item.Name);
            item.State = DownloadItem.States.Downloading;
            queryUpdate.Add(item);

            Log("Start Download " + item.ID);
            _lastReadList = new List<double>();
            _lastCurrent = 0;

            System.IO.Stream s = null;

            IDownloader downloader = DownloadHelper.GetDownloader(item);

            try
            {
                s = await downloader.GetDownloadStream(item, profile);
            }
            catch (Exception e)
            {
                Log("State Error - " + item.ID.ToString() + " - " + item.Name);
                item.State = DownloadItem.States.Error;
                queryUpdate.Add(item);
                queryAdd.Add(new DownloadError(3, item, e));
                await SocketHandler.Instance.SendIDError(item);
                _isDownloading = false;
                Log("Stream Problem");
                Log("DOWNLOAD FALSE");
                return;
            }
            Log("Got Stream " + item.ID);

            string groupDir = System.IO.Path.Combine(_settings.DownloadFolder, group.Name);
            string filesDir = System.IO.Path.Combine(_settings.DownloadFolder, group.Name, "files");
            string extractDir = System.IO.Path.Combine(_settings.DownloadFolder, group.Name, "extracted");

            try
            {
                if (!System.IO.Directory.Exists(groupDir))
                    System.IO.Directory.CreateDirectory(groupDir);

                if (!System.IO.Directory.Exists(filesDir))
                    System.IO.Directory.CreateDirectory(filesDir);

                if (!System.IO.Directory.Exists(extractDir))
                    System.IO.Directory.CreateDirectory(extractDir);
            }
            catch (Exception e)
            {
                Log("State Error - " + item.ID.ToString() + " - " + item.Name);
                item.State = DownloadItem.States.Error;
                queryUpdate.Add(item);
                await SocketHandler.Instance.SendIDError(item);

                queryAdd.Add(new DownloadError(4, item, e));
                Log("Error happened: " + e.Message);
                _isDownloading = false;
                return;
            }
            Log("Folders Checked " + item.ID + " - " + item.Name);


            string filePath = System.IO.Path.Combine(filesDir, item.Name);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate);

            Stopwatch sw = new Stopwatch();

            _current = 0;
            _state = new Timer(CheckStatus, item, 0, 500);
            Log("Timer started " + item.ID);

            sw.Start();
            try
            {
                const int buffersize = 1024 * 1024;
                byte[] buffer = new byte[buffersize];
                while (!_stopRequested)
                {
                    int readed = s.Read(buffer, 0, buffersize);
                    if (readed == 0) break;
                    _current += readed;

                    fs.Write(buffer, 0, readed);
                }
            }
            catch (Exception e)
            {
                sw.Stop();
                await SocketHandler.Instance.SendIDError(item);
                item.State = DownloadItem.States.Error;
                Log("State Error - " + item.ID.ToString() + " - " + item.Name);
                queryUpdate.Add(item);
                queryAdd.Add(new DownloadError(5, item, e));
                _isDownloading = false;
                return;
            }
            sw.Stop();

            fs.Close();
            fs.Dispose();
            _state.Dispose();
            _state = null;
            Log("fs + state disposed");

            Log("SW stopped");
            StatisticModel stat = new StatisticModel();
            stat.EntityID = item.ID;
            stat.EntityType = "ItemDownloadTime";
            stat.Value = sw.ElapsedTicks;
            stat.Source = StatisticModel.SourceType.Item;
            Log("stat erstellt");
            queryAdd.Add(stat);
            Log("stat in query");
            sw = null;


            if (_stopRequested)
            {
                _stopRequested = false;
                Log("Cancel requested");
                item.State = DownloadItem.States.Waiting;
                Log("State Waiting - " + item.ID.ToString() + " - " + item.Name);
                queryUpdate.Add(item);
                _isDownloading = false;
                return;
            }

            Log("Download finished " + item.ID);

            if (item.MD5 == null)
            {
                await SocketHandler.Instance.SendIDDownloaded(item);
                Log("State Downloaded - " + item.ID.ToString() + " - " + item.Name);
                item.State = DownloadItem.States.Downloaded;
                Log("Md5 not set " + item.ID);
                queryUpdate.Add(item);
                _isDownloading = false;

                return;
            }


            await SocketHandler.Instance.SendIDCheck(item);

            Log("Check MD5 " + item.ID);
            string hash = "";
            using (var md5 = MD5.Create())
            using (var stream = System.IO.File.OpenRead(filesDir + System.IO.Path.DirectorySeparatorChar + item.Name))
                hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();

            if (item.MD5 == hash)
            {
                await SocketHandler.Instance.SendIDDownloaded(item);
                item.State = DownloadItem.States.Downloaded;
            }
            else
            {
                await SocketHandler.Instance.SendIDError(item);
                item.State = DownloadItem.States.Error;
                Log("MD5 Error - " + item.ID.ToString() + " - " + item.Name);
                queryAdd.Add(new DownloadError(6, item, new Exception("MD5 Check failed")));
            }

            Log("Md5 checked " + item.ID);

            try
            {
                queryUpdate.Add(item);
            }
            catch (Exception e)
            {
                Log("Update Error - " + e.Message + " - " + item.Name);
            }
            _isDownloading = false;

            Log("Added to query download completed " + item.ID);
        }
    }
}
