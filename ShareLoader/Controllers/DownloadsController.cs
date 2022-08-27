using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using ShareLoader.Models;
using System.Text.RegularExpressions;

namespace ShareLoader.Controllers;

public class DownloadsController : Controller
{
    private readonly DownloadContext _context;
    private readonly ILogger<DownloadsController> _logger;
    
    public static CheckViewModel latestCheck;

    public DownloadsController(DownloadContext context, ILogger<DownloadsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(_context.Groups.ToList());
    }

    public IActionResult Add()
    {
        if(latestCheck == null) return NotFound();

        ViewData["OMDB_APIKEY"] = EnvironmentHelper.GetVariable("OMDB_APIKEY");


        Match m = new Regex(@"[a-z \.\\\/\(\-]((19|20)[0-9]{2})[a-z \.\\\/\)\-]").Match(latestCheck.Name);

        if (m.Success)
        {
            string search = latestCheck.Name.Substring(0, latestCheck.Name.LastIndexOf(m.Groups[1].Value) - 1).Replace('.', ' ');
            if (search.EndsWith(' '))
                search = search.Substring(0, search.ToString().Length);
            latestCheck.Search = search;
            latestCheck.Type = DownloadType.Movie;
        }
        else
        {
            m = new Regex("(English|German)").Match(latestCheck.Name);
            if (m.Success)
            {
                string search = latestCheck.Name.Substring(0, latestCheck.Name.LastIndexOf(m.Value)).Replace('.', ' ');
                if (search.EndsWith(' '))
                    search = search.Substring(0, search.ToString().Length - 1);
                latestCheck.Search = search;
                latestCheck.Type = DownloadType.Movie;
            }
        }

        m = new Regex(@"S([0-9]{1,2})[a-z \.\\\/\)\-]").Match(latestCheck.Name);
        if (m.Success)
        {
            string search = latestCheck.Name.Substring(0, latestCheck.Name.LastIndexOf(m.Value)).Replace('.', ' ');
            if (search.EndsWith(' '))
                search = search.Substring(0, search.ToString().Length - 1);
            latestCheck.Search = search;
            latestCheck.Type = DownloadType.Soap;
        }

        return View(latestCheck);
    }

    public async Task<IActionResult> GetItemInfo(string url)
    {
        IDownloadManager down = DownloadHelper.GetDownloader(url);
        string id = down.GetItemId(url);
        ItemModel item = await down.GetItemInfo(id);
        return Ok(item);
    }

    public IActionResult Delete(int Id)
    {
        DownloadGroup group = _context.Groups.SingleOrDefault(g => g.Id == Id);
        if(group == null) return NotFound();
        _context.Groups.Remove(group);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Detail(int Id)
    {
        DownloadGroup group = _context.Groups.SingleOrDefault(g => g.Id == Id);
        if(group == null) return NotFound();

        IEnumerable<DownloadItem> items = _context.Items.Where(i => i.DownloadGroupID == group.Id);
        int size = 0;
        foreach(DownloadItem item in items)
            size += item.Size;

        ViewData["totalSize"] = DownloadHelper.GetSizeString(size);
        ViewData["Items"] = items;

        return View(group);
    }

    //[HttpPost]
    public IActionResult ApiAdd(List<string> links)
    {
        links = new List<string>() {
            "https://ddownload.com/oo2muu8db32u/WsmdS.web.18p.tscc.S04E01.part1.rar",
            "https://ddownload.com/anedz7ctzci2/WsmdS.web.18p.tscc.S04E01.part2.rar",
            "https://ddownload.com/3rvox4e181n6/WsmdS.web.18p.tscc.S04E01.part3.rar",
            "https://ddownload.com/sswi6pbcdw6i/WsmdS.web.18p.tscc.S04E01.part4.rar"
        };
        latestCheck = new CheckViewModel() {
            Name = "Wer.stiehlt.mir.die.Show.S02.WebHD.HDR",
            RawLinks  = string.Join(',', links)
        };
        return RedirectToAction("Add");
    }
}
