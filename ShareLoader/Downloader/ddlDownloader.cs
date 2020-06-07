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
    public class ddlDownloader : IDownloader
    {
        HttpClient clientPublic = new HttpClient();
        public bool IsFree { get; } = false;
        public string Identifier { get; } = "ddl";
        public string UrlIdentifier { get; } = "ddownload.com";


        public async Task<DownloadItem> GetitemInfo(string id)
        {
            return await GetItemInfo(new DownloadItem() { ShareId = id });
        }

        public async Task<DownloadItem> GetItemInfo(DownloadItem item)
        {
            string status = await clientPublic.GetStringAsync("https://ddownload.com/" + item.ShareId);

            if(status.Contains("<h2>File Not Found</h2>"))
            {
                item.MD5 = "error";
                return item;
            }

            item.Hoster = "ddl";


            Regex reg = new Regex("<div class=\"name position-relative\">							<h4>([a-zA-Z0-9_\\-+\\.]+\\.[a-z]+)<\\/h4>						<\\/div>");

            Match m = reg.Match(status.Replace("\n", ""));
            if (m.Success)
            {
                item.Name = m.Groups[1].Value;
            }

            reg = new Regex("<span class=\"file-size\">([0-9.]+) ([A-Z]+)</span>");
            m = reg.Match(status);
            if (m.Success)
            {
                float size = float.Parse(m.Groups[1].Value.Replace(".", ","));
                if (m.Groups[2].Value == "MB")
                    size = size * 1024 * 1024;
                else if(m.Groups[2].Value == "GB")
                    size = size * 1024 * 1024 * 1024;
                item.Size = (int)size;
            }

            return item;
        }

        public async Task<AccountProfile> DoLogin(AccountProfile profile)
        {
            string login = await profile.Client.GetStringAsync("https://ddownload.com/login.html");

            Regex reg = new Regex("name=\"token\" value=\"([a-z0-9]*)\">");
            Match m = reg.Match(login);

            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("op", "login");
            paras.Add("rand", "");
            paras.Add("redirect", "https://ddownload.com/");
            paras.Add("token", m.Groups[0].Value);
            paras.Add("user", profile.Model.Username);
            paras.Add("user", profile.Model.Username);
            paras.Add("user", profile.Model.Username);
            paras.Add("user", profile.Model.Username);
            paras.Add("pass", profile.Model.Password);

            try
            {
                HttpResponseMessage resp = await profile.Client.PostAsync("https://ddownload.com/", new FormUrlEncodedContent(paras));
            }
            catch (Exception)
            {

            }
            return profile;
        }

        public async Task<AccountModel> GetAccountInfo(AccountProfile profile)
        {
            string profileStr = "";

            profileStr = await profile.Client.GetStringAsync("https://ddownload.com/?op=my_account");

            if (!profileStr.Contains("Affiliate link"))
            {
                profile.Model.IsPremium = false;
                profile.Model.TrafficLeft = 0;
                profile.Model.TrafficLeftWeek = 0;
                return profile.Model;
            }

            if (profileStr.Contains("value=\"(Free Account)\""))
            {
                profile.Model.IsPremium = false;
                profile.Model.TrafficLeft = 0;
                profile.Model.TrafficLeftWeek = 0;
                return profile.Model;
            }

            profile.Model.IsPremium = true;
            Regex reg = new Regex("<div class=\"price\"><sup>$</sup>([0-9]*)0</div>");
            float guthaben = float.Parse(reg.Match(profileStr).Groups[1].Value.Trim().Replace('.', ','));
            profile.Model.Credit = guthaben;

            if (profile.Model.IsPremium)
            {
                reg = new Regex("Premium Account (bis ([0-9]{1,2} [a-zA-Z]* [0-9]{4}))");
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
            Regex reg = new Regex("(http|https)://ddownload.com/([a-zA-Z0-9]+)");
            Match m = reg.Match(url);
            if (m.Success)
                return m.Groups[2].Value;
            else
                return url.Substring(url.LastIndexOf('/') + 1);
        }
    }
}
