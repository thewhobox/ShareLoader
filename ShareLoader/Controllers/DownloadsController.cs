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
        latestCheck = null;
        return RedirectToAction("Detail", new { id = group.Id });
    }

    public IActionResult Detail(int Id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == Id);
        if(group == null) return NotFound();

        IEnumerable<DownloadItem> items = _context.Items.Where(i => i.DownloadGroupID == group.Id);
        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        long size = 0;
        foreach(DownloadItem item in items)
        {
            size += item.Size;
            ViewData[item.ItemId] = System.IO.File.Exists(System.IO.Path.Combine(downloadPath, item.DownloadGroupID.ToString(), "files", item.Name));
        }

        ViewData["totalSize"] = DownloadHelper.GetSizeString(size);
        ViewData["Items"] = items;

        return View(group);
    }

    public IActionResult Errors(int id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == id);
        if(group == null) return NotFound();
        List<ErrorModel> errors = _context.Errors.Where(e => e.GroupId == id).ToList();
        ViewData["GroupId"] = id;
        ViewData["GroupName"] = group.Name;
        return View(errors);
    }

    public async Task<IActionResult> GetSeasons(string id)
    {
        string omdbapi = EnvironmentHelper.GetVariable("OMDB_APIKEY");
        string response = await new HttpClient().GetStringAsync("https://www.omdbapi.com/?apikey=" + omdbapi + "&i=" + id);
        return Ok(response);
    }

    public async Task<IActionResult> Reset(int id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == id);
        if(group == null) return NotFound();

        foreach(DownloadItem item in _context.Items.Where(i => i.DownloadGroupID == group.Id))
        {
            item.State = States.Waiting;
            _context.Items.Update(item);
            await Background.BackgroundTasks.Instance.StopDownload(item);
        }
        _context.SaveChanges();

        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings").DownloadFolder;
        string groupPath = System.IO.Path.Combine(downloadPath, group.Id.ToString());
        System.IO.Directory.Delete(groupPath, true);

        return RedirectToAction("Detail", new { id = group.Id });
    }

    public async Task<IActionResult> ResetItem(int id)
    {
        DownloadItem? item = _context.Items.SingleOrDefault(i => i.Id == id);
        if(item == null) return NotFound();
        item.State = States.Waiting;
        _context.Items.Update(item);
        _context.SaveChanges();
        
        await Background.BackgroundTasks.Instance.StopDownload(item);

        return RedirectToAction("Detail", new { id = item.DownloadGroupID });
    }

    public  IActionResult Edit(int id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(i => i.Id == id);
        if(group == null) return NotFound();
        return View(group);
    }

    [HttpPost]
    public  IActionResult Edit(DownloadGroup g)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(i => i.Id == g.Id);
        if(group == null) return NotFound();

        group.Name = g.Name;
        group.Password = g.Password;
        group.Type = g.Type;
        group.Sort = g.Sort;

        _context.Groups.Update(group);
        _context.SaveChanges();
        return RedirectToAction("Detail", new { id = g.Id });
    }


    public async Task<IActionResult> GetSearchResults(string query, string type, string page)
    {
        string omdbapi = EnvironmentHelper.GetVariable("OMDB_APIKEY");
        string response = await new HttpClient().GetStringAsync("https://www.omdbapi.com/?apikey=" + omdbapi + "&s=" + query + "&type=" + type + "&page=" + page);
        return Ok(response);
    }

    public async Task<IActionResult> GetItemInfo(string url)
    {
        IDownloadManager down = DownloadHelper.GetDownloader(url);
        try{
            string id = down.GetItemId(url);
            ItemModel item = await down.GetItemInfo(id);
            return Ok(item);
        } catch (Exception ex) {

        }
        ItemModel error = new ItemModel();
        error.Url = url;
        error.IsOnline = false;
        error.Downloader = down.Identifier;
        return Ok(error);
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

        try
        {
            Directory.Delete(Path.Combine(downloadPath, group.Id.ToString()), true);
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            foreach(string file in Directory.GetFiles(Path.Combine(downloadPath, group.Id.ToString())))
            {
                System.IO.File.Delete(file);
            }
            try
            {
                Directory.Delete(Path.Combine(downloadPath, group.Id.ToString()), true);
            } catch(Exception ex2) {
                Console.WriteLine(ex2.Message);
                return DeleteError(group.Id);
            }
        }

        return RedirectToAction("Index");
    }

    public IActionResult DeleteItem(int Id)
    {
        DownloadItem? item = _context.Items.SingleOrDefault(i => i.Id == Id);
        if(item == null) return NotFound();
        if(item.State == States.Downloading) return NotFound("Datei kann nicht gelöscht werden, während sie heruntergeladen wird.");
        if(item.State == States.Extracting) return NotFound("Datei kann nicht gelöscht werden, während sie entpackt wird.");

        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings")?.DownloadFolder ?? "";
        string filePath = System.IO.Path.Combine(downloadPath, item.DownloadGroupID.ToString(), "files", item.Name);
        try{
            if(System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        } catch(Exception ex) {
            return StatusCode(500, "Server konnte die Datei nicht löschen: \r\n" + ex.Message);
        }

        //item.State = States.Waiting;
        //_context.Items.Update(item);
        //_context.SaveChanges();

        return RedirectToAction("Detail", new { id = item.DownloadGroupID });
    }

    public IActionResult DeleteError(int Id)
    {
        ErrorModel? item = _context.Errors.SingleOrDefault(i => i.Id == Id);
        if(item == null) return NotFound();
        
        _context.Errors.Remove(item);
        _context.SaveChanges();

        return RedirectToAction("Errors", new { id = item.GroupId });
    }

    public IActionResult Pause(int id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == id);
        if(group == null) return NotFound();
        group.State = GroupStates.Paused;
        _context.Groups.Update(group);
        _context.SaveChanges();
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
    
    public IActionResult UnPause(int id)
    {
        DownloadGroup? group = _context.Groups.SingleOrDefault(g => g.Id == id);
        if(group == null) return NotFound();
        group.State = GroupStates.Normal;

        _context.Groups.Update(group);
        _context.SaveChanges();
        return RedirectToAction("Detail", new { id = id });
    }

    public IActionResult UnPauseItem(int id)
    {
        DownloadItem? item = _context.Items.SingleOrDefault(i => i.Id == id);
        if(item == null) return NotFound();

        string downloadPath = SettingsHelper.GetSetting<SettingsModel>("settings")?.DownloadFolder ?? "";
        string filePath = System.IO.Path.Combine(downloadPath, item.DownloadGroupID.ToString(), "files", item.Name);
        string extractPath = System.IO.Path.Combine(downloadPath, item.DownloadGroupID.ToString(), "extracted", item.GroupID.ToString());

        if(!System.IO.File.Exists(filePath))
        {
            item.State = States.Waiting;
        } else {
            FileInfo info = new FileInfo(filePath);
            if(info.Length == item.Size) {
                bool flag = false;
                if(System.IO.Directory.Exists(extractPath))
                {
                    foreach(string file in System.IO.Directory.GetFiles(extractPath))
                    {
                        info = new FileInfo(file);
                        if(file.Length > (1024 * 1024))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if(flag)
                    item.State = States.Extracted;
                else
                    item.State = States.Downloaded;
            } else {
                item.State = States.Waiting;
            }
        }

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
            await Background.BackgroundTasks.Instance.StopExtract(item);
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

    [HttpPost]
    public async Task<IActionResult> ApiAdd()
    {
        string links = "";
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            links = await reader.ReadToEndAsync();
        }
        System.Console.WriteLine("ApiAdd2");
        byte[] linksb64 = System.Convert.FromBase64String(links);
        var plainTextBytes = System.Text.Encoding.UTF8.GetString(linksb64);
        System.Console.WriteLine(plainTextBytes);
        latestCheck = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckViewModel>(plainTextBytes);
        return Ok("ShareLoader");
    }
    
    public IActionResult ApiFile(IFormFile file)
    {
        System.Console.WriteLine("ApiFile");
        string content;
        using(StreamReader reader = new StreamReader(file.OpenReadStream()))
        {
            content = reader.ReadToEnd();
        }
        if(file.FileName.EndsWith(".dlc"))
            latestCheck = DecryptHelper.DecryptContainer(content);
        else {
            CheckViewModel model = new CheckViewModel();
            model.Name = "Einzellinks";
            model.RawLinks = content.Replace("\r\n", ",").Replace("\r", ",");
            latestCheck = model;
        }
        return RedirectToAction("Add");
    }
}
