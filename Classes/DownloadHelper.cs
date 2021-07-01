using ShareLoader.Downloader;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class DownloadHelper
    {
        public static List<string> GetAllHoster(bool? onlyFree = null)
        {
            List<string> temp = new List<string>();
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.IsNested == false && t.Namespace == "ShareLoader.Downloader"
                    select t;

            foreach (Type t in q.ToList())
            {
                IDownloader down = (IDownloader)Activator.CreateInstance(t);


                if (onlyFree != null && down.IsFree != onlyFree)
                    continue;

                temp.Add(down.Identifier);
            }

            return temp;
        }

        public static List<string> GetAllHosterNames(bool? onlyFree = null)
        {
            List<string> temp = new List<string>();
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.IsNested == false && t.Namespace == "ShareLoader.Downloader"
                    select t;

            foreach (Type t in q.ToList())
            {
                IDownloader down = (IDownloader)Activator.CreateInstance(t);


                if (onlyFree != null && down.IsFree != onlyFree)
                    continue;

                temp.Add(down.UrlIdentifier);
            }

            return temp;
        }

        public static IDownloader GetDownloader(DownloadItem item)
        {
            IDownloader downloader = null;

            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.IsNested == false && t.Namespace == "ShareLoader.Downloader"
                    select t;

            foreach (Type t in q.ToList())
            {
                IDownloader down = (IDownloader)Activator.CreateInstance(t);
                if (item.Hoster == down.Identifier)
                {
                    downloader = down;
                    break;
                }
            }

            return downloader;
        }

        public static IDownloader GetDownloader(string item)
        {
            IDownloader downloader = null;

            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.Namespace == "ShareLoader.Downloader" && !t.FullName.Contains("+")
                    select t;

            foreach(Type t in q.ToList())
            {
                IDownloader down = (IDownloader)Activator.CreateInstance(t);
                if (item.Contains(down.UrlIdentifier) || item == down.Identifier)
                {
                    downloader = down;
                    break;
                }
            }

            return downloader;
        }

        public static IDownloader GetDownloader(AccountProfile profile)
        {
            if (profile == null) return null;
            IDownloader downloader = null;

            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.IsNested == false && t.Namespace == "ShareLoader.Downloader"
                    select t;

            foreach (Type t in q.ToList())
            {
                IDownloader down = (IDownloader)Activator.CreateInstance(t);
                if (profile.Model.Hoster == down.Identifier)
                {
                    downloader = down;
                    break;
                }
            }

            return downloader;
        }
        
        public static DownloadGroup SortGroups(DownloadGroup group)
        {
            Regex regPart = new Regex(@"(.+)\.part[0-9]{0,4}\.rar$");
            Regex regR0 = new Regex(@"(.+)\.r[0-9]{0,4}$");

            int currentGroup = 0;
            while (true)
            {
                currentGroup += 1;
                DownloadItem tempItem = null;
                try
                {
                    tempItem = group.Items.First(i => i.GroupID == -1);
                }
                catch (Exception) { break; }

                tempItem.GroupID = currentGroup;

                Match mPart = regPart.Match(tempItem.Name);
                Match mR0 = regR0.Match(tempItem.Name);
                

                if(mPart.Success)
                {
                    string partname = mPart.Groups[1].Value;

                    foreach (DownloadItem toEdit in group.Items.Where(i => i.Name.StartsWith(partname)))
                    {
                        toEdit.GroupID = currentGroup;
                    }
                }

                if (mR0.Success)
                {
                    string partname = mR0.Groups[1].Value;

                    foreach (DownloadItem toEdit in group.Items.Where(i => i.Name.StartsWith(partname)))
                    {
                        toEdit.GroupID = currentGroup;
                    }
                }


                
            }

            return group;
        }
    }
}
