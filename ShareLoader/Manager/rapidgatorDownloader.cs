using Newtonsoft.Json.Linq;
using ShareLoader.Classes;
using ShareLoader.Models;
using ShareLoader.Share;
using System.Text.RegularExpressions;

namespace ShareLoader.Manager;

public class rapidgatorDownloader : IDownloadManager
{
    public string Identifier { get; } = "rpg";

    public bool Check(string url)
    {
        return url.Contains("rapidgator.net") || url.Contains("rg.to/file");
    }

    public string GetItemId(string url)
    {
        Regex reg = new Regex("(http|https)://(rapidgator.net|rg.to)/file/([a-zA-Z0-9]+)");
        Match m = reg.Match(url);
        if (m.Success)
            return m.Groups[3].Value;
            
        url = url.Substring(0, url.IndexOf('?'));
        return url.Substring(url.LastIndexOf('/') + 1);
    }

    public async Task<ItemModel> GetItemInfo(string Id)
    {
        ItemModel item = new ItemModel() { Id = Id };

        Background.AccountChecker account = Background.BackgroundTasks.Instance.account;
        AccountProfile? profile = account.GetProfileAny("rpg");
        if(profile == null) {
            item.IsOnline = false;
            return item;
        }

        HttpClient clientPublic = new HttpClient();

        string response = await clientPublic.GetStringAsync($"https://rapidgator.net/api/v2/file/info?token={profile.Model.Token}&file_id={Id}");
        JObject jresp = JObject.Parse(response);
        if(jresp["status"].ToString() != "200")
        {
            item.IsOnline = false;
            return item;
        }

        item.IsOnline = true;
        item.Name = jresp["response"]["file"]["name"].ToString();
        item.Size = long.Parse(jresp["response"]["file"]["size"].ToString());
        item.Downloader = "rpg";
        item.Url = $"https://rapidgator.net/file/{Id}";
        return item;
    }

    public async Task<bool> CheckStreamRange(DownloadItem item, AccountProfile profile)
    {
        try {
            string response = await profile.Client.GetStringAsync($"https://rapidgator.net/api/v2/file/download?file_id={item.ItemId}&token={profile.Model.Token}");
            JObject jresp = JObject.Parse(response);
            if(jresp["status"].ToString() != "200")
                return false;
            
            HttpResponseMessage resp = await profile.Client.GetAsync(jresp["response"]["download_url"].ToString(), HttpCompletionOption.ResponseHeadersRead);
            return resp.Headers.AcceptRanges?.Contains("bytes") == true;
        } catch{

        }
        return false;
    }

    public async Task<Stream> GetDownloadStream(DownloadItem item, AccountProfile profile, long start = 0)
    {
        Stream? s = null;
        try {
            string response = await profile.Client.GetStringAsync($"https://rapidgator.net/api/v2/file/download?file_id={item.ItemId}&token={profile.Model.Token}");
            JObject jresp = JObject.Parse(response);
            if(jresp["status"].ToString() != "200")
                return s;
            string file_url = jresp["response"]["download_url"].ToString();


            System.Console.WriteLine($"URL: {file_url}");
            if(start == 0)
            {
                s = await profile.Client.GetStreamAsync(file_url);
            }
            else {
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, file_url);
                req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, item.Size);
                HttpResponseMessage resp = await profile.Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
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
        string response = await acc.Client.GetStringAsync($"https://rapidgator.net/api/v2/user/info?token={acc.Model.Token}");
        JObject jresp = JObject.Parse(response);
        if(jresp["status"].ToString() != "200")
        {
            acc.Model.IsPremium = false;
            acc.Model.TrafficLeft = 0;
            acc.Model.TrafficLeftWeek = 0;
            acc.Model.Credit = 0;
            return;
        }
        
        acc.Model.Credit = 0;
        acc.Model.IsPremium = (bool)(jresp["response"]["user"]["is_premium"] ?? false);
        if(acc.Model.IsPremium)
        {
            acc.Model.TrafficLeft = long.Parse(jresp["response"]["user"]["traffic"]["left"].ToString());
            acc.Model.TrafficLeftWeek = acc.Model.TrafficLeft;
            acc.Model.ValidTill = DateTime.UnixEpoch.AddSeconds(long.Parse(jresp["response"]["user"]["premium_end_time"].ToString()));
        } else {
            acc.Model.TrafficLeft = 0;
            acc.Model.TrafficLeftWeek = 0;
        }
    }

    public async Task<bool> DoLogin(AccountProfile acc)
    {
        string response = await acc.Client.GetStringAsync($"https://rapidgator.net/api/v2/user/login?login={acc.Model.Username}&password={acc.Model.Password}");
        JObject jresp = JObject.Parse(response);
        if(jresp["status"].ToString() != "200")
            return false;

        acc.Model.Token = jresp["response"]["token"].ToString();
        return true;
    }
}
