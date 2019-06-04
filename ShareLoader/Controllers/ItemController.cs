using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Models;

namespace ShareLoader.Controllers
{
    public class ItemController : Controller
    {
        private readonly DownloadContext _context;
        private readonly DownloadSettings _settings;

        public ItemController(DownloadContext context, DownloadSettings settings)
        {
            _context = context;
            _settings = settings;
        }

        public IActionResult Statistics(int id)
        {
            List<StatisticModel> items = _context.Statistics.Where(i => i.EntityType == "ItemAverageDownload" && i.EntityID == id).ToList();

            StatisticShowModel m = new StatisticShowModel() { itemid = id };
            StatisticDataset data = new StatisticDataset() { label = "Geschwindigkeit MB/s" };

            for (int i = 0; i < items.Count; i++)
            {
                if(i == 0 || i == items.Count - 1)
                    m.data.labels.Add(items[i].Stamp.ToString());
                else
                    m.data.labels.Add("");

                data.data.Add(items[i].Value);
            }

            m.data.datasets.Add(data);

            return View(m);
        }

        public IActionResult Errors(int id)
        {
            DownloadItem item = _context.Items.Single(i => i.ID == id);

            ErrorViewModel m = new ErrorViewModel
            {
                Errors = _context.Errors.Where(e => e.ItemID == id).ToList(),
                ItemId = id,
                Group = _context.Groups.Single(g => g.ID == item.DownloadGroupID)
            };
            return View(m);
        }

        public IActionResult Reset(int id)
        {
            DownloadItem item = _context.Items.Single(i => i.ID == id);
            DownloadGroup group = _context.Groups.Single(g => g.ID == item.DownloadGroupID);

            string filePath = System.IO.Path.Combine(_settings.DownloadFolder, group.Name, "files", item.Name);
            try
            {
                System.IO.File.Delete(filePath);
            }
            catch { }

            SocketHandler.Instance.SendIDReset(item);

            item.State = DownloadItem.States.Waiting;
            _context.Update(item);
            _context.RemoveRange(_context.Statistics.Where(s => s.Source == StatisticModel.SourceType.Item && s.EntityID == id));
            _context.SaveChanges();

            return RedirectToAction("Show", "Downloads", new { id = group.ID });
        }
    }
}