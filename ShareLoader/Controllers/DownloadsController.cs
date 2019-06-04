using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Models;
using System.ComponentModel;
using ShareLoader.Data;
using System.Text;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using ShareLoader.Classes;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ShareLoader.Manager;
using ShareLoader.Downloader;
using System.Text.RegularExpressions;

namespace ShareLoader.Controllers
{
    public class DownloadsController : Controller
    {
        private readonly DownloadContext _context;
        private DownloadSettings _settings;

        public DownloadsController(DownloadContext context)
        {
            _context = context;

            _settings = DownloadSettings.Load();
        }
        
        public IActionResult Index()
        {
            Dictionary<string, StatisticShowModel> models = new Dictionary<string, StatisticShowModel>();

            StatisticShowModel model = new StatisticShowModel() { type = "bar", id = "statChartAccounts" };
            IEnumerable<AccountModel> accounts = _context.Accounts.ToList();
            Dictionary<int, int> IdToIndex = new Dictionary<int, int>();
            int index = 0;

            model.data.labels.Add("24h");
            //model.data.labels.Add("72h");

            foreach (AccountModel acc in accounts)
            {
                StatisticDataset data = new StatisticDataset() { label = acc.Username };
                data.fill = true;

                if (!_context.Statistics.Any(s => s.EntityType == "AccountVolDay" && s.EntityID == acc.ID))
                    continue;

                StatisticModel stat1 = _context.Statistics.Where(s => s.EntityType == "AccountVolDay" && s.EntityID == acc.ID).OrderByDescending(s => s.Stamp).First();
                //StatisticModel stat2 = _context.Statistics.Where(s => s.EntityType == "AccountVolWeek" && s.EntityID == acc.ID).OrderByDescending(s => s.Stamp).First();
                data.data.Add(Math.Round(stat1.Value, 0));
                //data.data.Add(Math.Round(stat2.Value, 0));

                model.data.datasets.Add(data);
                IdToIndex.Add(acc.ID, index);
                index++;
            }
            models.Add("accounts", model);

            ViewData["idToIndex"] = Newtonsoft.Json.JsonConvert.SerializeObject(IdToIndex);

            model = new StatisticShowModel() { id = "statChartDownload" };
            StatisticDataset datal = new StatisticDataset() { label = "Geschwindigkeit MB/s" };
            model.data.labels.Add(DateTime.Now.ToString());
            datal.data.Add(0);
            model.data.datasets.Add(datal);
            models.Add("download", model);

            return View(models);
        }

        public IActionResult Edit(int id)
        {
            if (!_context.Groups.Any(g => g.ID == id))
                return NotFound();

            DownloadGroup group = _context.Groups.Single(g => g.ID == id);
            return View(group);
        }

        [HttpPost]
        public IActionResult Edit(DownloadGroup group)
        {
            _context.Groups.Update(group);
            _context.SaveChanges();
            return RedirectToAction("Show", new { id = group.ID });
        }

        public IActionResult List()
        {
            List<DownloadGroup> groups = _context.Groups.ToList();
            foreach (DownloadGroup g in groups)
            {
                g.Items = _context.Items.Where(i => i.DownloadGroupID == g.ID).ToList();
            }
            return View(groups);
        }

        public IActionResult Show(int id)
        {
            if (!_context.Groups.Any(g => g.ID == id))
                return NotFound();
            
            ViewData["groupId"] = id;

            ShowGroup model = new ShowGroup();
            model.Group = _context.Groups.Single(g => g.ID == id);
            model.Items = new List<DownloadItem>();

            List<DownloadItem> items = _context.Items.Where(i => i.DownloadGroupID == id).ToList();

            foreach (DownloadItem item in items)
            {
                if(item.State == DownloadItem.States.Downloading || item.State == DownloadItem.States.Error)
                {
                    string path = System.IO.Path.Combine(_settings.DownloadFolder, model.Group.Name, "files", item.Name);
                    long size = System.IO.File.Exists(path) ? new System.IO.FileInfo(path).Length : 0;

                    int perc = (int)(size*100 / item.Size);
                    item.SetPercentage(perc);
                }
                model.Items.Add(item);
            }



            model.Items = _context.Items.Where(i => i.DownloadGroupID == id).OrderBy(i => i.Name).ToList();

            return View(model);
        }
        
        public IActionResult Delete(int id)
        {
            DownloadGroup group = _context.Groups.Single(g => g.ID == id);
            _context.Groups.Remove(group);

            IEnumerable<StatisticModel> stats = _context.Statistics.Where(s => s.EntityID == id && s.Source == StatisticModel.SourceType.Item);
            _context.Statistics.RemoveRange(stats);

            _context.SaveChanges();
            return RedirectToAction("List");
        }

        public IActionResult Reset(int id)
        {
            DownloadGroup group = _context.Groups.Single(g => g.ID == id);

            IEnumerable<DownloadItem> items = _context.Items.Where(i => i.DownloadGroupID == id);
            DownloadManagerService._stopRequested = true;

            foreach (DownloadItem item in items)
            {
                item.State = DownloadItem.States.Waiting;
                _context.Items.Update(item);

                try
                {
                    string filePath = System.IO.Path.Combine(_settings.DownloadFolder, group.Name, "files", item.Name);
                    System.IO.File.Delete(filePath);
                }
                catch { }

            }

            _context.SaveChanges();

            return RedirectToAction("Show", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> AddDlc(IFormFile Dlc)
        {
            AddDlcModel model = await DlcHelper.ParseDlc(Dlc.OpenReadStream());
            if (model == null)
                RedirectToAction("Index"); // TODO show error message

            Match m = new Regex(@"[a-z \.\\\/\(\-]((19|20)[0-9]{2})[a-z \.\\\/\)\-]").Match(model.Name);

            if (m.Success)
            {
                string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Groups[1].Value) - 1).Replace('.', ' ');
                if (search.EndsWith(' '))
                    search = search.Substring(0, search.ToString().Length);
                ViewData["searchString"] = search;
                model.Type = DownloadType.Movie;
            }
            else
            {
                m = new Regex("(English|German)").Match(model.Name);
                if (m.Success)
                {
                    string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Value)).Replace('.', ' ');
                    if (search.EndsWith(' '))
                        search = search.Substring(0, search.ToString().Length - 1);
                    ViewData["searchString"] = search;
                    model.Type = DownloadType.Movie;
                }
            }

            m = new Regex(@"S([0-9]{1,2})[a-z \.\\\/\)\-]").Match(model.Name);
            if (m.Success)
            {
                string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Value)).Replace('.', ' ');
                if (search.EndsWith(' '))
                    search = search.Substring(0, search.ToString().Length - 1);
                ViewData["searchString"] = search;
                model.Type = DownloadType.Soap;
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddDlcConfirm(string[] items, AddDlcModel model)
        {
            DownloadGroup group = new DownloadGroup();
            group.Name = model.Name;
            group.Password = model.Password;
            group.Type = model.Type;
            group.Target = model.Target;
            group.Sort = model.nameToSort;

            if (items.Count() < 1)
                throw new Exception("No Items selected/found");

            IDownloader down;

            foreach (string itemStr in items)
            {
                DownloadItem item = new DownloadItem() { Url = itemStr };

                down = DownloadHelper.GetDownloader(itemStr);
                item.Hoster = down.Identifier;
                item.ShareId = down.GetItemId(itemStr);
                item = await down.GetItemInfo(item);
                group.Items.Add(item);
            }

            group = DownloadHelper.SortGroups(group);

            _context.Groups.Add(group);
            _context.SaveChanges();
            return RedirectToAction("Show", new { id = group.ID });
        }

        public IActionResult apiGetNewItem(int id)
        {
            DownloadItem item = _context.Items.Single(i => i.ID == id);
            DownloadGroup group = _context.Groups.Single(g => g.ID == item.DownloadGroupID);

            Dictionary<string, string> ret = new Dictionary<string, string>();

            string url = Url.Action("Show", new { id = group.ID });

            ret.Add("group", "<a href='" + url + "'>" + group.Name + "</a>");
            ret.Add("file", item.Name);

            return Json(ret);
        }

        public IActionResult Exit()
        {
            Response.Redirect("/", false);
            Environment.Exit(10);
            return Ok();
        }
    }
}
