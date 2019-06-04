using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ShareLoader.Classes;
using ShareLoader.Classes.Exceptions;
using ShareLoader.Models;

namespace ShareLoader.Downloader
{
    public class ShareDownloader : IDownloader
    {
        HttpClient clientPublic = new HttpClient();
        public bool IsFree { get; } = false;
        public string Identifier { get; } = "so";
        public string UrlIdentifier { get; } = "share-online.biz";


        public async Task<DownloadItem> GetitemInfo(string id)
        {
            return await GetItemInfo(new DownloadItem() { ShareId = id });
        }

        public async Task<DownloadItem> GetItemInfo(DownloadItem item)
        {
            string status = await clientPublic.GetStringAsync("http://api.share-online.biz/linkcheck.php?md5=1&links=" + item.ShareId);
            string[] infos = status.Split(';');
            item.Hoster = "so";

            if (infos.Count() > 1 && infos[1] == "OK")
            {
                item.Name = infos[2];
                int.TryParse(infos[3], out int size);
                item.Size = size;
                item.MD5 = infos[4];

                if(item.Size == 0)
                {
                    int.TryParse(infos[4], out size);
                    item.Size = size;
                    if (size == 0)
                        item.MD5 = "error";
                }
            }
            else
            {
                item.MD5 = "error";
            }

            return item;
        }

        public async Task<AccountProfile> DoLogin(AccountProfile profile)
        {
            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("user", profile.Model.Username);
            paras.Add("pass", profile.Model.Password);

            try
            {
                HttpResponseMessage resp = await profile.Client.PostAsync("https://www.share-online.biz/user/login", new FormUrlEncodedContent(paras));
            }
            catch (Exception)
            {

            }
            return profile;
        }

        public async Task<AccountModel> GetAccountInfo(AccountProfile profile)
        {
            string profileStr = "";

            profileStr = await profile.Client.GetStringAsync("https://www.share-online.biz/user/profile");

            if (!profileStr.Contains("willkommen im Benutzerbereich"))
            {
                profile.Model.IsPremium = false;
                profile.Model.TrafficLeft = 0;
                profile.Model.TrafficLeftWeek = 0;
                return profile.Model;
            }

            if (!profileStr.Contains("Premium        </p>"))
            {
                profile.Model.IsPremium = false;
                profile.Model.TrafficLeft = 0;
                profile.Model.TrafficLeftWeek = 0;
                return profile.Model;
            }

            Regex reg = new Regex("Ihr Account-Typ:\n        <\\/p>\n        <p class=\"p_r\">\n(.*)<\\/p>");
            string type = reg.Match(profileStr).Groups[1].Value.Trim();
            profile.Model.IsPremium = type == "Premium" || type == "Penalty-Premium";



            reg = new Regex("Aktuelles Guthaben <span class=\"sm\">\\(\\*\\*\\*\\)<\\/span>:\n        <\\/p>\n        <p class=\"p_r\">\n(.*)&euro;");
            float guthaben = float.Parse(reg.Match(profileStr).Groups[1].Value.Trim().Replace('.', ','));
            profile.Model.Credit = guthaben;

            if (profile.Model.IsPremium)
            {
                reg = new Regex("Account gültig bis:\n        <\\/p>\n        <p class=\"p_r\">\n            <span class='green'>(.*)<\\/span>");
                string validdate = reg.Match(profileStr).Groups[1].Value.Trim();
                profile.Model.ValidTill = DateTime.Parse(validdate);

                reg = new Regex("1 Tag: <img src='(.*)' alt='' width='16' height='16' title='([0-9]*)% verfügbar \\((.*) GiB\\)");
                float traffic1 = float.Parse(reg.Match(profileStr).Groups[3].Value.Trim().Replace('.', ','));

                reg = new Regex("7 Tage: <img src='(.*)' alt='' width='16' height='16' title='([0-9]*)% verfügbar \\((.*) GiB\\)");
                float traffic7 = float.Parse(reg.Match(profileStr).Groups[3].Value.Trim().Replace('.', ','));

                profile.Model.TrafficLeft = traffic1;
                profile.Model.TrafficLeftWeek = traffic7;
            }
            else
            {
                profile.Model.TrafficLeft = 0;
                profile.Model.TrafficLeftWeek = 0;
            }
            return profile.Model;
        }

        public async Task<Stream> GetDownloadStream(DownloadItem item, AccountProfile profile)
        {
            string content = await profile.Client.GetStringAsync("http://www.share-online.biz/dl/" + item.ShareId);
            Regex reg = new Regex("var dl=\"(.*)\";var file");
            Match m = reg.Match(content);

            if (!m.Success)
                throw new NoDownloadException();

            var encodedBytes = System.Convert.FromBase64String(m.Groups[1].Value);
            string url = System.Text.Encoding.UTF8.GetString(encodedBytes);

            Stream s = await profile.Client.GetStreamAsync(url);
            return s;
        }

        public string GetItemId(string url)
        {
            Regex reg = new Regex("(http|https)://www.share-online.biz/dl/([a-zA-Z0-9]+)");
            Match m = reg.Match(url);
            if (m.Success)
                return m.Groups[2].Value;
            else
                return url.Substring(url.LastIndexOf('/') + 1);
        }
    }
}
