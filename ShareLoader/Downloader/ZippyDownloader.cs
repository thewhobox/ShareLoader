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
    public class ZippyDownloader : IDownloader
    {
        HttpClient clientPublic = new HttpClient();
        public bool IsFree { get; } = true;
        public string Identifier { get; } = "zs";
        public string UrlIdentifier { get; } = "zippyshare.com";
        public bool AllowClientRedirect { get; } = true;

        public Task<AccountProfile> DoLogin(AccountProfile profile)
        {
            throw new NotImplementedException();
        }

        public Task<AccountModel> GetAccountInfo(AccountProfile profile)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> GetDownloadStream(DownloadItem item, AccountProfile profile)
        {
            Exception e = new Exception();
            string content = "";
            int count = 0;
            while(count < 3)
            {
                try
                {
                    content = await profile.Client.GetStringAsync(item.Url);
                    break;
                } catch (Exception ex)
                {
                    e = ex;
                }
                count++;
            }

            if (content == "")
                throw new Exception("Too many exceptions: " + e.Message);

            Regex reg = new Regex("document\\.getElementById\\(\'dlbutton\'\\)\\.href = \"\\/d\\/" + item.ShareId + "\\/\" \\+ \\(([0-9]*) % ([0-9]*) \\+ ([0-9]*) % ([0-9]*)\\) \\+ \"\\/");
            Match m = reg.Match(content);

            int ground = int.Parse(m.Groups[1].Value);
            int mod1 = int.Parse(m.Groups[2].Value);
            int mod2 = int.Parse(m.Groups[4].Value);

            int num1 = ground % mod1;
            int num2 = ground % mod2;

            int fin = num1 + num2;

            reg = new Regex("\\) \\+ \"\\/ (.*)\";");
            m = reg.Match(content);
            
            string url = item.Url.Substring(0, 28) + "/d/" + item.ShareId + "/" + fin + "/" + item.Name.Replace(" ", "%20");

            return await profile.Client.GetStreamAsync(url);
        }

        public async Task<DownloadItem> GetItemInfo(DownloadItem item)
        {
            string content = await clientPublic.GetStringAsync(item.Url);

            Regex reg = new Regex("document\\.getElementById\\('dlbutton'\\)\\.href = \"(.*)\\/\" \\+ \\((.*)\\) \\+ \"\\/(.*)\";");
            item.Name = System.Web.HttpUtility.UrlDecode(reg.Match(content).Groups[3].Value);

            reg = new Regex("Size:<\\/font>            <font style=\"line-height:18px; font-size: 13px;\">(.*)<\\/font>");
            string[] size = reg.Match(content).Groups[1].Value.Split(' ');
            if (size[1] == "MB")
                item.Size =  (int)float.Parse(size[0].Replace('.', ',')) * 1024 * 1024;
            else
                item.Size = (int)float.Parse(size[0].Replace('.', ',')) * 1024 * 1024 * 1024;

            item.MD5 = null;
            item.Hoster = "zs";
            item.ShareId = GetItemId(item.Url);

            return item;
        }

        public string GetItemId(string url)
        {
            url = url.Replace("/file.html", "");
            return url.Substring(url.LastIndexOf('/') + 1);
        }
    }
}
//    m/v/Z3PXYk66/file.html