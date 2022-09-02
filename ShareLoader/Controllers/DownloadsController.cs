using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using ShareLoader.Models;
using ShareLoader.Share;
using System.Text.RegularExpressions;

namespace ShareLoader.Controllers;

public class DownloadsController : Controller
{
    private readonly DownloadContext _context;
    private readonly ILogger<DownloadsController> _logger;
    
    public static CheckViewModel? latestCheck;

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

        return View(latestCheck);
    }

    [HttpPost]
    public IActionResult Add(CheckViewModel model)
    {
        DownloadGroup group = new DownloadGroup(model);
        _context.Groups.Add(group);
        _context.SaveChanges();

        foreach(ItemModel item in model.Links)
        {
            DownloadItem ditem = new DownloadItem(item);
            ditem.DownloadGroupID = group.Id;
            _context.Items.Add(ditem);
        }

        _context.SaveChanges();
        return RedirectToAction("Detail", new { id = group.Id });
    }

    public IActionResult Reset(int id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == id);
        if(group == null) return NotFound();

        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        string groupPath = System.IO.Path.Combine(downloadPath, group.Id.ToString());
        //TODO stop download
        System.IO.Directory.Delete(groupPath, true);

        foreach(DownloadItem item in _context.Items.Where(i => i.DownloadGroupID == group.Id))
        {
            item.State = States.Waiting;
            _context.Items.Update(item);
        }
        _context.SaveChanges();
        return RedirectToAction("Detail", new { id = group.Id });
    }

    public IActionResult ResetItem(int id)
    {
        DownloadItem? item = _context.Items.SingleOrDefault(i => i.Id == id);
        if(item == null) return NotFound();
        item.State = States.Waiting;
        _context.Items.Update(item);
        _context.SaveChanges();
        
        //TODO stop download
        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        string filePath = System.IO.Path.Combine(downloadPath, item.DownloadGroupID.ToString(), "files", item.Name);
        if(System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
        return RedirectToAction("Detail", new { id = item.DownloadGroupID });
    }

    public async Task<IActionResult> GetItemInfo(string url)
    {
        IDownloadManager down = DownloadHelper.GetDownloader(url);
        string id = down.GetItemId(url);
        ItemModel item = await down.GetItemInfo(id);
        return Ok(item);
    }

    public IActionResult GetGroupsInfo()
    {
        List<GroupInfo> infos = new List<GroupInfo>();
        foreach(DownloadGroup group in _context.Groups)
        {
            GroupInfo info = new GroupInfo();
            info.Id = group.Id;
            info.Downloaded = _context.Items.Count(i => i.DownloadGroupID == group.Id && i.State == States.Downloaded);
            info.Extracted = _context.Items.Count(i => i.DownloadGroupID == group.Id && i.State == States.Extracted);
            info.Finished = _context.Items.Count(i => i.DownloadGroupID == group.Id && i.State == States.Finished);
            info.Error = _context.Items.Count(i => i.DownloadGroupID == group.Id && i.State == States.Error);
            infos.Add(info);
        }
        return Ok(infos);
    }

    public async Task<IActionResult> Delete(int Id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == Id);
        if(group == null) return NotFound();
        _context.Groups.Remove(group);
        IEnumerable<DownloadItem> items = _context.Items.Where(i => i.DownloadGroupID == group.Id).ToList();
        _context.Items.RemoveRange(items);
        _context.SaveChanges();


        foreach(DownloadItem item in items)
            await Background.BackgroundTasks.Instance.StopDownload(item);
            
        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        System.IO.Directory.Delete(System.IO.Path.Combine(downloadPath, group.Id.ToString()), true);

        return RedirectToAction("Index");
    }

    public IActionResult Detail(int Id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == Id);
        if(group == null) return NotFound();

        IEnumerable<DownloadItem> items = _context.Items.Where(i => i.DownloadGroupID == group.Id);
        int size = 0;
        foreach(DownloadItem item in items)
            size += item.Size;

        ViewData["totalSize"] = DownloadHelper.GetSizeString(size);
        ViewData["Items"] = items;

        return View(group);
    }

    public IActionResult Pause(int id)
    {
        return RedirectToAction("Detail", new { id = id });
    }

    public IActionResult PauseItem(int id)
    {
        DownloadItem? item = _context.Items.SingleOrDefault(i => i.Id == id);
        if(item == null) return NotFound();
        if(item.State == States.Downloading) return NotFound("DownloadItem kann nicht zurück gesetzt werden, solange es downloaded.");
        if(item.State == States.Extracting) return NotFound("DownloadItem kann nicht zurück gesetzt werden, solange es entpackt.");
        if(item.State == States.Moving) return NotFound("DownloadItem kann nicht zurück gesetzt werden, solange es verschoben wird.");
        if(item.State == States.Finished) return NotFound("DownloadItem kann nicht zurück gesetzt werden, es ist fertig.");

        item.State = States.Paused;
        _context.Items.Update(item);
        _context.SaveChanges();

        return RedirectToAction("Detail", new { id = item.DownloadGroupID });
    }

    public async Task<IActionResult> StopItem(int id)
    {
        DownloadItem? item = _context.Items.SingleOrDefault(i => i.Id == id);
        if(item == null) return NotFound();

        if(item.State == States.Downloading)
        {
            await Background.BackgroundTasks.Instance.StopDownload(item);
        } else if(item.State == States.Extracting)
        {

        } else {
             return NotFound("DownloadItem kann nur gestoppt werden, wenn es heruntergeladen oder entpackt wird.");
        }

        item.State = States.Error;
        _context.Items.Update(item);
        _context.SaveChanges();

        return RedirectToAction("Detail", new { id = item.DownloadGroupID });
    }

    public IActionResult ApiTest()
    {
        return Ok("ShareLoader");
    }

    //[HttpPost]
    public IActionResult ApiAddTest(List<string> links)
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

    public IActionResult ApiAdd(string links)
    {
        System.Console.WriteLine("ApiAdd2");
        byte[] linksb64 = System.Convert.FromBase64String(links);
        var plainTextBytes = System.Text.Encoding.UTF8.GetString(linksb64);
        System.Console.WriteLine(plainTextBytes);
        latestCheck = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckViewModel>(plainTextBytes);
        return Ok("ShareLoader");
    }
}
