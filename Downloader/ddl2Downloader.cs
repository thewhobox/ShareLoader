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
    public class ddl2Downloader : IDownloader
    {
        HttpClient clientPublic = new HttpClient();
        public bool IsFree { get; } = false;
        public string Identifier { get; } = "ddl";
        public string UrlIdentifier { get; } = "ddl.to";
        public bool AllowClientRedirect { get; } = false;


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
            paras.Add("token", m.Groups[1].Value);
            paras.Add("login", profile.Model.Username);
            paras.Add("password", profile.Model.Password);

            try
            {
                HttpResponseMessage resp = await profile.Client.PostAsync("https://ddownload.com/", new FormUrlEncodedContent(paras));
                string x = await resp.Content.ReadAsStringAsync();
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
            Regex reg = new Regex("<div class=\"price\">" + @"<sup>($|€){1}</sup>([0-9]+)</div>");
            float guthaben = float.Parse(reg.Match(profileStr).Groups[2].Value.Trim().Replace('.', ','));
            profile.Model.Credit = guthaben;

            if (profile.Model.IsPremium)
            {
                //"Premium Account (expires 6 July 2020)" >
                reg = new Regex("Premium Account \\(expires ([0-9]{1,2} [a-zA-Z]* [0-9]{4})\\)");
                string validdate = reg.Match(profileStr).Groups[1].Value.Trim();
                profile.Model.ValidTill = DateTime.Parse(validdate);

                reg = new Regex("<div class=\"price\"><sup>MB</sup>([0-9]+)</div>");
                float traffic1 = float.Parse(reg.Match(profileStr).Groups[1].Value.Trim().Replace('.', ',')) / 1024;
                float traffic7 = 700 - traffic1;

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
            string content = await profile.Client.GetStringAsync("https://ddownload.com/" + item.ShareId);

            Dictionary<string, string> paras = new Dictionary<string, string>();
            paras.Add("op", "download2");
            paras.Add("id", item.ShareId);
            paras.Add("rand", "");
            paras.Add("referer", "");
            paras.Add("method_free", "");
            paras.Add("method_premium", "1");
            paras.Add("adblock_detected", "0");

            HttpResponseMessage mesg = await profile.Client.PostAsync("https://ddownload.com/" + item.ShareId, new FormUrlEncodedContent(paras));

            Stream s = await profile.Client.GetStreamAsync(mesg.Headers.Location);
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
