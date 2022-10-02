using Newtonsoft.Json.Linq;
using ShareLoader.Classes;
using ShareLoader.Models;
using ShareLoader.Share;
using System.Text.RegularExpressions;

namespace ShareLoader.Manager;

public class ddlDownloader : IDownloadManager
{
    public string Identifier { get; } = "ddl";

    public bool Check(string url)
    {
        return url.Contains("ddownload.com");
    }

    public string GetItemId(string url)
    {
        Regex reg = new Regex("(http|https)://ddownload.com/([a-zA-Z0-9]+)");
        Match m = reg.Match(url);
        if (m.Success)
            return m.Groups[2].Value;
        else
            return url.Substring(url.LastIndexOf('/') + 1);
    }

    public async Task<ItemModel> GetItemInfo(string Id)
    {
        ItemModel item = new ItemModel() { Id = Id };
        HttpClient clientPublic = new HttpClient();

        string apiKey = EnvironmentHelper.GetVariable("DDL_APIKEY");
        if(string.IsNullOrEmpty(apiKey))
        {
            System.Diagnostics.Debug.WriteLine("Kein API-KEY für ddownload");
            return null;
        }

        string response = await clientPublic.GetStringAsync($"https://api-v2.ddownload.com/api/file/info?key={apiKey}&file_code={Id}");
        JObject jresp = JObject.Parse(response);
        item.IsOnline = jresp["result"][0]["status"].ToString() == "200";
        item.Downloader = "ddl";
        item.Url = "https://ddownload.com/" + Id;
        if(item.IsOnline)
        {
            item.Name = jresp["result"][0]["name"].ToString();
            item.Size = int.Parse(jresp["result"][0]["size"].ToString());
        }

        return item;
    }

    public async Task<bool> CheckStreamRange(DownloadItem item, AccountProfile profile)
    {
        try {
            HttpResponseMessage resp = await profile.Client.GetAsync("https://ddownload.com/" + item.ItemId);
            resp = await profile.Client.GetAsync(resp.Headers.Location, HttpCompletionOption.ResponseHeadersRead);
            return resp.Headers.AcceptRanges?.Contains("bytes") == true;
        } catch{

        }
        return false;
    }

    public async Task<Stream> GetDownloadStream(DownloadItem item, AccountProfile profile, long start = 0)
    {
        Stream? s = null;
        try {
            HttpResponseMessage resp = await profile.Client.GetAsync("https://ddownload.com/" + item.ItemId);
            if(start == 0)
                s = await profile.Client.GetStreamAsync(resp.Headers.Location);
            else {
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, resp.Headers.Location);
                req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, item.Size);
                resp = await profile.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                s = resp.Content.ReadAsStream();
            }
        } catch(Exception ex) {
            System.Console.WriteLine(ex.Message);
            System.Console.WriteLine(ex.StackTrace);
        }
        return s;
    }

    public async Task GetAccounInfo(AccountProfile acc)
    {
        string profileStr = "";
        try{
            profileStr = await acc.Client.GetStringAsync("https://ddownload.com/?op=my_account");
        } catch{
            return;
        }

        if (!profileStr.Contains("Affiliate link"))
        {
            acc.Model.IsPremium = false;
            acc.Model.TrafficLeft = 0;
            acc.Model.TrafficLeftWeek = 0;
            return;
        }

        if (profileStr.Contains("value=\"(Free Account)\""))
        {
            acc.Model.IsPremium = false;
            acc.Model.TrafficLeft = 0;
            acc.Model.TrafficLeftWeek = 0;
            return;
        }
        
        acc.Model.IsPremium = true;
        Regex reg = new Regex("<div class=\"price\">" + @"<sup>($|€){1}</sup>([0-9]+)</div>");
        float guthaben = float.Parse(reg.Match(profileStr).Groups[2].Value.Trim().Replace('.', ','));
        acc.Model.Credit = guthaben;

        reg = new Regex("Premium Account \\(expires ([0-9]{1,2} [a-zA-Z]* [0-9]{4})\\)");
        string validdate = reg.Match(profileStr).Groups[1].Value.Trim();
        acc.Model.ValidTill = DateTime.Parse(validdate);

        reg = new Regex("<div class=\"price\"><sup>MB</sup>([0-9]+)</div>");
        float traffic1 = float.Parse(reg.Match(profileStr).Groups[1].Value.Trim().Replace('.', ',')) * 1024 * 1024;
        float traffic7 = 700 - traffic1;

        acc.Model.TrafficLeft = traffic1;
        acc.Model.TrafficLeftWeek = traffic7;
    }

    public async Task<bool> DoLogin(AccountProfile acc)
    {
        string login = await acc.Client.GetStringAsync("https://ddownload.com/login.html");


        Dictionary<string, string> paras = new Dictionary<string, string>();
        paras.Add("op", "login");
        paras.Add("redirect", "https://ddownload.com/");
        paras.Add("login", acc.Model.Username);
        paras.Add("password", acc.Model.Password);
            
        Regex reg = new Regex("name=\"token\" value=\"([a-z0-9]*)\">");
        Match m = reg.Match(login);
        paras.Add("token", m.Groups[1].Value);
        reg = new Regex("name=\"rand\" value=\"([a-z0-9]*)\">");
        m = reg.Match(login);
        paras.Add("rand", m.Groups[1].Value);

        try
        {
            HttpResponseMessage resp = await acc.Client.PostAsync("https://ddownload.com/", new FormUrlEncodedContent(paras));
            string x = await resp.Content.ReadAsStringAsync();
            if(x.Contains("class='alert alert-danger'"))
            {
                reg = new Regex("class='alert alert-danger'>([A-Za-z0-9 ]*)</div>");
                m = reg.Match(x);
                Console.WriteLine("Fehler: " + m.Groups[1].Value);
                return false;
            }
            bool success = !x.Contains("Login");
            System.Console.WriteLine("Login: " + (success ? "erfolgreich" : "fehlerhaft"));
            return success;
        }
        catch (Exception)
        {
            return false;
        }
    }
}