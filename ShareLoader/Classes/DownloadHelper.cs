using ShareLoader.Manager;
using ShareLoader.Models;
using System.Reflection;

namespace ShareLoader.Classes;

public class DownloadHelper
{
    public static string GetSizeString(double size)
    {
        string[] format = new string[] { "{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB" };
        int i = 0;
        while (i < format.Length && size >= 1024)
        {
            size = (long)(100 * size / 1024) / 100.0;
            i++;
        }
        return string.Format(format[i], size);
    }

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