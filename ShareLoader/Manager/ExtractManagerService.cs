using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShareLoader.Manager
{
    public class ExtractManagerService : BackgroundService
    {
        public readonly DownloadContext _context;
        public readonly DownloadSettings _settings;
        private bool isExtracting = false;
        private List<string> _standardOutput = new List<string>();

        public ExtractManagerService(DownloadSettings settings, IConfiguration configuration)
        {
            _settings = settings;
            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();

            //optionsBuilder.UseMySql("server=teamserver;userid=admin;password=Mein#pw#mysqladmin;database=shareloader;");
            optionsBuilder.UseMySql(configuration.GetConnectionString("MySQLConnection"));

            _context = new DownloadContext(optionsBuilder.Options);
        }
        

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalExtract), stoppingToken);

                if(_settings.ExtractImmedialy)
                {
                    await CheckExtract(); //TODO remove async
                    continue;
                }

                if (isExtracting) //if (DownloadManagerService._isDownloading || isExtracting)
                    continue;

                isExtracting = true;

                List<string> hosterFree = DownloadHelper.GetAllHoster(true);

                bool existWaitingFreeDownload = _context.Items.Any(i => i.State == DownloadItem.States.Waiting && hosterFree.Contains(i.Hoster));
                if (existWaitingFreeDownload)
                    continue;

                List<string> hosterPayed = DownloadHelper.GetAllHoster(false);
                List<string> hosterAll = DownloadHelper.GetAllHoster();

                bool hasTraffic = false;
                foreach(string hoster in hosterPayed)
                {
                    if (!_context.Accounts.Any(a => a.Hoster == hoster && a.TrafficLeft > 1))
                        continue;

                    hasTraffic = true;
                }
                
                if(hasTraffic)
                {
                    bool hasSomethingToDownload = false;

                    foreach(DownloadGroup group in _context.Groups.Where(g => !g.IsTemp))
                    {
                        if (_context.Items.Any(i => i.DownloadGroupID == group.ID && (i.State == DownloadItem.States.Waiting || i.State == DownloadItem.States.Downloading)))
                            hasSomethingToDownload = true;
                    }

                    if (!hasSomethingToDownload)
                        CheckExtract();
                    else
                        isExtracting = false;

                } else {
                    isExtracting = false;
                }
            }
        }

        public async Task CheckExtract()
        {
            DownloadItem _item = null;
            List<DownloadItem> _items = null;

            bool flagStartExtract = false;
            
            foreach (DownloadGroup group in _context.Groups.Where(g => g.IsTemp == false).ToList())
            {
                int groupCount = _context.Items.Where(i => i.DownloadGroupID == group.ID).GroupBy(i => i.GroupID).Count();
                
                for(int gid = 1; gid <= groupCount; gid++)
                {
                    if (_context.Items.Any(i => i.DownloadGroupID == group.ID && i.GroupID == gid && i.State != DownloadItem.States.Downloaded))
                        continue;
                    
                    _items = _context.Items.Where(i => i.DownloadGroupID == group.ID && i.GroupID == gid).ToList();

                    flagStartExtract = true;
                    DoExtract(_items, group);
                }
            }

            if (!flagStartExtract)
                isExtracting = false;
        }

        public async Task DoExtract(List<DownloadItem> itemsGroup, DownloadGroup group)
        {
            DownloadItem item = itemsGroup.OrderBy(i => i.Name).First();

            string firstPart = item.Name;

            //TODO check if neccessary

            Regex reg = new Regex(@"\.part[0]{0,}1\.rar");
            Match m = reg.Match(item.Name);
            if (!m.Success && itemsGroup.Count > 1)
            {
                reg = new Regex(@"(.+)\.r[0-9]{0,4}$");
                if (reg.IsMatch(itemsGroup[1].Name))
                {
                    foreach(DownloadItem ti in itemsGroup)
                    {
                        if (ti.Name.EndsWith(".rar"))
                            firstPart = ti.Name;
                    }
                }
            }

            foreach (DownloadItem i in itemsGroup)
            {
                await SocketHandler.Instance.SendIDExtract(i);
                i.State = DownloadItem.States.Extracting;
                _context.Items.Update(i);
            }
            await _context.SaveChangesAsync();

            string fileDir = Path.Combine(_settings.DownloadFolder, group.Name, "files") + Path.DirectorySeparatorChar + firstPart;
            string extractDir = Path.Combine(_settings.DownloadFolder, group.Name, "extracted", item.GroupID.ToString());
            string args = _settings.ZipArgs.Replace("%filesDir%", fileDir).Replace("%extractDir%", extractDir).Replace("%pass%", group.Password);

            if (!string.IsNullOrEmpty(group.Password))
                args += " -p\"" + group.Password + "\""; //TODO remove

            Process p = new Process();
            p.StartInfo.FileName = _settings.ZipPath;
            p.StartInfo.Arguments = args;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.Start();


            StartOutStandard(p, Path.Combine(_settings.LogFolder, group.Name, "log-" + item.GroupID.ToString() + "-extract.log"), item);
            
            p.WaitForExit();

            if(p.ExitCode != 0)
            {
                bool flag = false;
                foreach(string line in _standardOutput)
                {
                    if(line.Contains("ERROR"))
                    {
                        DownloadError error = new DownloadError(7, item, new Exception(line));
                        _context.Errors.Add(error);
                        flag = true;
                    }
                }

                if(!flag)
                {
                    DownloadError error = new DownloadError(8, item, new Exception(string.Join(Environment.NewLine, _standardOutput)));
                    _context.Errors.Add(error);
                }
                await SocketHandler.Instance.SendIDError(item);
                item.State = DownloadItem.States.Error;
                _context.Items.Update(item);
                foreach (DownloadItem i in itemsGroup)
                {
                    if (i == item) continue;
                    i.State = DownloadItem.States.Downloaded;
                    _context.Items.Update(i);
                }
            } else
            {
                await SocketHandler.Instance.SendIDExtracted(item);

                foreach (DownloadItem i in itemsGroup)
                {
                    i.State = DownloadItem.States.Extracted;
                    _context.Items.Update(i);
                    //Todo delete files
                    System.IO.File.Delete(Path.Combine(_settings.DownloadFolder, group.Name, "files", i.Name));
                }
            }
            
            await _context.SaveChangesAsync();
            isExtracting = false;
        }

        private async void StartOutStandard(Process p, string path, DownloadItem item)
        {
            try
            {

                StreamWriter w = new StreamWriter(path);
                char[] buffer = new char[1024];
                string currentLine = "";
                Regex reg = new Regex("^[ ]{0,2}([0-9]{1,3})%");

                while (!p.HasExited)
                {
                    int readed = p.StandardOutput.Read(buffer, 0, 1024);

                    foreach (char c in buffer)
                    {
                        if (c != '\0' && c != '\n')
                        {
                            if (c == '\r')
                            {
                                Match m = reg.Match(currentLine);

                                if (m.Success)
                                {
                                    if(m.Groups[1].Value != "0")
                                        await SocketHandler.Instance.SendIDExtract(item, m.Groups[1].Value);
                                }
                                else
                                    _standardOutput.Add(currentLine);

                                currentLine = "";
                            }
                            else
                                currentLine = currentLine + c.ToString();
                        }
                    }

                    w.Write(buffer);
                    w.Flush();
                }

                w.WriteLine("ExitCode: " + p.ExitCode.ToString());
                await w.FlushAsync();
                w.Dispose();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}