using ShareLoader.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class AddDlcModel
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string Comments { get; set; }
        public string nameToSort { get; set; }
        public DownloadType Type { get; set; }
        public DownloadTarget Target { get; set; }

        public Dictionary<int, AddDlcGroupModel> Items { get; set; } = new Dictionary<int, AddDlcGroupModel>();
    }

    public class AddDlcGroupModel
    {
        public List<DownloadItem> Items { get; set; } = new List<DownloadItem>();

        public string GetId()
        {
            return Items[0].GroupID.ToString();
        }

        public string GetTitle()
        {
            string title = Items[0].Name;

            Regex regPart = new Regex(@"(.+)\.part[0-9]{0,4}\.rar$");
            Regex regR0 = new Regex(@"(.+)\.r[0-9]{0,4}$");

            Match mPart = regPart.Match(title);
            Match mR0 = regR0.Match(title);


            if (mPart.Success)
                return mPart.Groups[1].Value;

            if (mR0.Success)
                return mR0.Groups[1].Value;

            return title.Substring(0, title.LastIndexOf('.'));
        }

        public string GetClass()
        {
            if (Items.Where(i => i.MD5 == "error").Count() == Items.Count)
                return "bg-red fg-white";
            if (Items.Any(i => i.MD5 == "error"))
                return "bg-orange fg-white";
            return "bg-green fg-white";
        }
    }
}
