using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ShareLoader.CNL.Models;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ShareLoader.CNL.Controllers;

public class flashController : Controller
{
    private readonly ILogger<flashController> _logger;

    public flashController(ILogger<flashController> logger)
    {
        _logger = logger;
    }

    public static string MainUrl { get; set; } = "";
    public static bool updated = false;

    static CheckViewModel model;

    public async Task<IActionResult> Settings()
    {
        if(updated)
        {
            ViewData["updated"] = "true";
            updated = false;
        }


        if(System.IO.File.Exists("settings.txt"))
        {
            string defHost = System.IO.File.ReadAllText("settings.txt");
            MainUrl = defHost;
            ViewData["MainUrl"] = defHost;
        }

        if(string.IsNullOrEmpty(MainUrl))
        {
            HttpClient client = new HttpClient();
            List<string> hosts = new List<string>() { 
                "http://localhost:5162", 
                "http://qnap:5162" 
            };
            if(System.IO.File.Exists("settings.txt"))
            {
                string defHost = System.IO.File.ReadAllText("settings.txt");
                if(!hosts.Contains(defHost))
                    hosts.Add(defHost);
            }
            foreach(string url in hosts)
            {
                try{
                    string answer = await client.GetStringAsync(url + "/Downloads/ApiTest");
                    if(answer == "ShareLoader")
                    {
                        ViewData["auto"] = "true";
                        ViewData["updated"] = "false";
                        MainUrl = url;
                        break;
                    }
                } catch {}
            }
        }

        ViewData["MainUrl"] = MainUrl;
        return View();
    }

    [HttpPost]
    public IActionResult Settings(string url)
    {
        MainUrl = url;
        System.IO.File.WriteAllText("settings.txt", MainUrl);
        ViewData["MainUrl"] = MainUrl;
        updated = true;
        return RedirectToAction("Settings");
    }

    public async Task<IActionResult> addcrypted2()
    {
        string jk = Request.Form["jk"];
        jk = jk.Substring(jk.IndexOf('\'') + 1);
        jk = jk.Substring(0, jk.IndexOf('\''));
        string passwords = Request.Form["passwords"];
        string source = Request.Form["source"];
        string package = Request.Form["package"];
        string crypted = Request.Form["crypted"];

        byte[] dataByte = Convert.FromBase64String(crypted);

        jk = jk.ToUpper();
        string decKey = "";
        for (int i = 0; i < jk.Length; i += 2)
        {
            decKey += (char)Convert.ToUInt16(jk.Substring(i, 2), 16);
        }

        string rawLinks = "";

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = System.Text.Encoding.ASCII.GetBytes(decKey);
            aesAlg.IV = System.Text.Encoding.ASCII.GetBytes(decKey);
            aesAlg.Padding = PaddingMode.None;
            aesAlg.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(dataByte))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        rawLinks = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        Regex rgx = new Regex("\u0000+$");
        rawLinks = rgx.Replace(rawLinks, "");
        string[] links = rawLinks.Split("\r\n");



        model = new CheckViewModel() { 
            Name = package,
            RawLinks = string.Join(",", links),
            RawLinksCount = links.Count()
        };

        Match m = new Regex(@"[a-z \.\\\/\(\-]((19|20)[0-9]{2})[a-z \.\\\/\)\-]").Match(model.Name);

        m = new Regex(@"S([0-9]{1,2})[a-z \.\\\/\)\-]").Match(model.Name);
        if (m.Success)
        {
            string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Value)).Replace('.', ' ');
            if (search.EndsWith(' '))
                search = search.Substring(0, search.ToString().Length - 1);
            model.Search = search;
            model.Type = DownloadType.Soap;
        }

        HttpClient client = new HttpClient();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(json);
        string linksb64 = System.Convert.ToBase64String(plainTextBytes);

        var res = await client.GetStringAsync(MainUrl + "/Downloads/ApiAdd?links=" + linksb64);

        var ps = new ProcessStartInfo(MainUrl + "/Downloads/Add")
        { 
            UseShellExecute = true, 
            Verb = "open" 
        };
        Process.Start(ps);

        return Ok();
    }

    public IActionResult check() 
    {
        return View(model);
    }

    public async Task<IActionResult> itemInfo(string Id, string domain)
    {
        ItemModel item = new ItemModel() { Id = Id };
        HttpClient clientPublic = new HttpClient();

        if(domain == "ddownload.com")
        {
            string response = await clientPublic.GetStringAsync("https://api-v2.ddownload.com/api/file/info?key=86749xuhs96bb63rc55kv&file_code=" + Id);
            JObject jresp = JObject.Parse(response);
            item.Name = jresp["result"][0]["name"].ToString();
            item.IsOnline = jresp["result"][0]["status"].ToString() == "200";
            item.Size = int.Parse(jresp["result"][0]["size"].ToString());
        }

        return Ok(item);
    }
}
