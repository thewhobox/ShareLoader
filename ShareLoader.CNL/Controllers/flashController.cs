using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.CNL.Classes;
using ShareLoader.Share;

namespace ShareLoader.CNL.Controllers;

public class flashController : Controller
{
    private readonly ILogger<flashController> _logger;

    public flashController(ILogger<flashController> logger)
    {
        _logger = logger;
    }

    public static bool updated = false;

    public async Task<IActionResult> Settings()
    {
        if(updated)
        {
            ViewData["updated"] = "true";
            updated = false;
        }

        string host = SettingsHelper.GetSetting("host");
        string download = SettingsHelper.GetSetting("download");

        if(string.IsNullOrEmpty(host))
        {
            HttpClient client = new HttpClient();
            List<string> hosts = new List<string>() { 
                "http://localhost:5162", 
                "http://qnap:5162" 
            };
            foreach(string url in hosts)
            {
                try{
                    string answer = await client.GetStringAsync(url + "/Downloads/ApiTest");
                    if(answer == "ShareLoader")
                    {
                        ViewData["auto"] = "true";
                        ViewData["updated"] = "false";
                        host = url;
                        SettingsHelper.SetSetting("host", url);
                        BackgroundWatcher.Instance?.StartWatcher();
                        break;
                    }
                } catch {}
            }
        }
        if(string.IsNullOrEmpty(download))
        {
            download = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            SettingsHelper.SetSetting("download", download);
            BackgroundWatcher.Instance?.StartWatcher();
        }

        ViewData["MainUrl"] = host;
        ViewData["DownloadPath"] = download;
        return View();
    }

    [HttpPost]
    public IActionResult Settings(string url, string download)
    {
        SettingsHelper.SetSetting("host", url);
        SettingsHelper.SetSetting("download", download);
        BackgroundWatcher.Instance?.StartWatcher();
        updated = true;
        return RedirectToAction("Settings");
    }

    public async Task<IActionResult> addcrypted2()
    {
        string? jk = Request.Form["jk"];
        if(string.IsNullOrEmpty(jk))
            return RedirectToAction("error", new { id = 2, message = "No jk found." });
        jk = jk.Substring(jk.IndexOf('\'') + 1);
        jk = jk.Substring(0, jk.IndexOf('\''));
        string passwords = Request.Form.ContainsKey("passwords") ? Request.Form["passwords"].ToString() : "";
        Console.WriteLine("Passwords: " + passwords);
        string? source = Request.Form["source"];
        Console.WriteLine("Source: " + source);
        string package = Request.Form.ContainsKey("package") ? Request.Form["package"].ToString() : "Unknown";
        string? crypted = Request.Form["crypted"];

        if(string.IsNullOrEmpty(crypted))
            return RedirectToAction("error", new { id = 2, message = "No crypted links found." });

        jk = jk.ToUpper();
        string decKey = "";
        for (int i = 0; i < jk.Length; i += 2)
        {
            decKey += (char)Convert.ToUInt16(jk.Substring(i, 2), 16);
        }

        string linksS = DecryptHelper.Decrypt(Convert.FromBase64String(crypted), decKey);
        Console.WriteLine(linksS);
        string[] links = linksS.Split("\r\n");
        Console.WriteLine("Links: " + links.Length);


        CheckViewModel model = new CheckViewModel() { 
            Name = package,
            RawLinks = string.Join(",", links)
        };

        SearchHelper.GetSearch(model);

        HttpClient client = new HttpClient();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(json);
        string linksb64 = System.Convert.ToBase64String(plainTextBytes);
        string host = SettingsHelper.GetSetting("host");

        try {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, host + "/Downloads/ApiAdd");
            requestMessage.Content = new StringContent(linksb64, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
            var res = await client.SendAsync(requestMessage);
            var ps = new ProcessStartInfo(host + "/Downloads/Add")
            { 
                UseShellExecute = true, 
                Verb = "open" 
            };
            Process.Start(ps);
        } catch(Exception ex) {
            Console.WriteLine("Host not reachable: " + ex.Message);
            var ps = new ProcessStartInfo("http://localhost:9666/flash/error/1?message=" + (System.Web.HttpUtility.UrlEncode(ex.Message) ?? "Undefined"))
            { 
                UseShellExecute = true, 
                Verb = "open" 
            };
            Process.Start(ps);
        }

        return Ok();
    }

    public IActionResult error(int id, string message)
    {
        switch(id)
        {
            case 1:
                ViewData["msg"] = "Der angegebene Host ist nicht erreichbar.";
                break;

            case 2:
                ViewData["msg"] = "Es trat ein Fehler beim Entschlüsseln der Links auf.";
                break;

            default:
                ViewData["msg"] = "Es trat ein unbekannter Fehler auf.";
                break;
        }
        ViewData["error"] = message;
        return View();
    }
}
