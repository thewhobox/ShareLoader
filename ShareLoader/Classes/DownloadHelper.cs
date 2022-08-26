using ShareLoader.Manager;
using ShareLoader.Models;
using System.Reflection;

namespace ShareLoader.Classes;

public class DownloadHelper
{
    public static IDownloadManager GetDownloader(string item)
    {
        IDownloadManager downloader = null;

        var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.Namespace == "ShareLoader.Manager" && !t.FullName.Contains("+")
                select t;

        foreach(Type t in q.ToList())
        {
            IDownloadManager down = (IDownloadManager)Activator.CreateInstance(t);
            if (down.Check(item))
            {
                downloader = down;
                break;
            }
        }

        return downloader;
    }

    public static IDownloadManager GetDownloader(AccountProfile profile)
    {
        if (profile == null) return null;
        IDownloadManager downloader = null;

        var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                where t.IsClass && t.IsNested == false && t.Namespace == "ShareLoader.Manager"
                select t;

        foreach (Type t in q.ToList())
        {
            IDownloadManager down = (IDownloadManager)Activator.CreateInstance(t);
            if (profile.Model.Hoster == down.Identifier)
            {
                downloader = down;
                break;
            }
        }

        return downloader;
    }
}