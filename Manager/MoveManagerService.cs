using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShareLoader.Manager
{
    public class MoveManagerService : BackgroundService
    {
        public readonly DownloadContext _context;
        public readonly DownloadSettings _settings;
        public bool _isMoving = false;

        public MoveManagerService(IConfiguration configuration)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>();
            //optionsBuilder.UseMySql("server=teamserver;userid=admin;password=Mein#pw#mysqladmin;database=shareloader;");
            //optionsBuilder.UseMySql(configuration.GetConnectionString("MySQLConnection"));
            optionsBuilder.UseSqlite("Data Source=database.db");

            _context = new DownloadContext(optionsBuilder.Options);
            _settings = DownloadSettings.Load();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalMove), stoppingToken);

                if (_isMoving)
                    continue;

                if (_settings.ExtractImmedialy)
                {
                    await CheckMove(); //TODO remove async
                    continue;
                }

                await CheckMove(); //TODO add checksystem
                continue;
            }
        }

        public async Task CheckMove()
        {
            DownloadItem _item = null;
            List<DownloadItem> _items = null;


            _isMoving = true;
            bool flag = false;

            foreach (DownloadGroup group in _context.Groups.Where(g => g.IsTemp == false).ToList())
            {
                int groupCount = _context.Items.Where(i => i.DownloadGroupID == group.ID).GroupBy(i => i.GroupID).Count();

                for (int gid = 1; gid <= groupCount; gid++)
                {
                    var it = _context.Items.Where(i => i.DownloadGroupID == group.ID && i.GroupID == gid);
                    if (_context.Items.Any(i => i.DownloadGroupID == group.ID && i.GroupID == gid && i.State != DownloadItem.States.Extracted))
                        continue;

                    _item = _context.Items.Where(i => i.DownloadGroupID == group.ID && i.GroupID == gid).OrderBy(i => i.Name).First();
                    _items = _context.Items.Where(i => i.DownloadGroupID == group.ID && i.GroupID == gid).ToList();

                    flag = true;
                    DoMove(_items, group);
                    return;
                }
            }

            if (!flag)
                _isMoving = false;
        }

        public async Task DoMove(List<DownloadItem> _items, DownloadGroup _group)
        {
            switch(_group.Type)
            {
                case DownloadType.Movie:
                    await MoveMovie(_items, _group);
                    break;
                case DownloadType.Soap:
                    await MoveSoap(_items, _group);
                    break;
                case DownloadType.Other:
                    await MoveOther(_items, _group);
                    break;
            }

            _isMoving = false;
        }

        public async Task MoveOther(List<DownloadItem> _items, DownloadGroup _group)
        {
            string pathFrom = System.IO.Path.Combine(_settings.DownloadFolder, _group.Name, "extracted", _items[0].GroupID.ToString());
            string[] files = Directory.GetFiles(pathFrom);
            string pathTo = Path.Combine(_settings.MoveFolder, "Other");

            if (!Directory.Exists(pathTo))
                Directory.CreateDirectory(pathTo);

            foreach (string file in files)
            {
                string fileTo = Path.Combine(pathTo, Path.GetFileName(file));
                if (File.Exists(fileTo))
                    File.Delete(fileTo);
                File.Move(file, fileTo);
            }

            await SocketHandler.Instance.SendIDFinish(_items[0]);

            foreach (DownloadItem item in _items)
                item.State = DownloadItem.States.Finished;

            _context.Items.UpdateRange(_items);
            _context.SaveChanges();
            _isMoving = false;
        }

        public async Task MoveMovie(List<DownloadItem> _items, DownloadGroup _group)
        {
            string pathFrom = System.IO.Path.Combine(_settings.DownloadFolder, _group.Name, "extracted", _items[0].GroupID.ToString());
            string[] files = Directory.GetFiles(pathFrom, "*.mkv");
            
            string fileToMove = "";

            foreach (string file in files)
            {
                if (Path.GetFileName(file).Contains("sample"))
                    continue;

                fileToMove = file;
                break;
            }

            string pathTo = Path.Combine(_settings.MoveFolder, "Filme");
            string fileTo = Path.Combine(pathTo, _group.Sort + Path.GetExtension(fileToMove));

            if (!Directory.Exists(pathTo))
                Directory.CreateDirectory(pathTo);
            
            try
            {
                if (File.Exists(fileTo))
                    File.Delete(fileTo);
                File.Move(fileToMove, fileTo);
            } catch(Exception e)
            {
                Console.WriteLine("Verschieben: " + e.Message);
            }


            await SocketHandler.Instance.SendIDFinish(_items[0]);

            foreach (DownloadItem item in _items)
                item.State = DownloadItem.States.Finished;

            _context.Items.UpdateRange(_items);
            _context.SaveChanges();
            _isMoving = false;
        }

        public async Task MoveSoap(List<DownloadItem> _items, DownloadGroup _group)
        {
            bool dyn = (_group.Sort.Substring(_group.Sort.LastIndexOf(" ") + 1)) == "dynamisch";
            
            if (dyn)
                _group.Sort = _group.Sort.Substring(0, _group.Sort.LastIndexOf("/"));
            else
                _group.Sort = _group.Sort.Replace('/', Path.DirectorySeparatorChar);
            
            string pathFrom = System.IO.Path.Combine(_settings.DownloadFolder, _group.Name, "extracted", _items[0].GroupID.ToString());
            string[] files = Directory.GetFiles(pathFrom, "*.mkv");


            foreach(string file in files)
            {

                string filename = Path.GetFileName(file);
                if (filename.Contains("sample"))
                    continue;

                string pathTo = "";

                if(dyn)
                {
                    Regex reg = new Regex(@"(?i)s([0-9]{1,2})[\.]{0,1}e[0-9]{1,2}");
                    Match m = reg.Match(filename);
                    pathTo = Path.Combine(_settings.MoveFolder, "Serien", _group.Sort, "Staffel " + m.Groups[1].Value);
                } else
                {
                    pathTo = Path.Combine(_settings.MoveFolder, "Serien", _group.Sort);
                }

                if(!Directory.Exists(pathTo))
                    Directory.CreateDirectory(pathTo);

                string endpath = Path.Combine(pathTo, filename);

                try
                {
                    if (File.Exists(endpath))
                        File.Delete(endpath);
                    File.Move(file, endpath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Verschieben: " + e.Message);
                }
            }

            await SocketHandler.Instance.SendIDFinish(_items[0]);

            foreach(DownloadItem item in _items)
                item.State = DownloadItem.States.Finished;

            _context.Items.UpdateRange(_items);
            _context.SaveChanges();

            _isMoving = false;
        }
    }
}
