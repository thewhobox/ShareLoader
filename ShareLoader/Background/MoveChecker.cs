using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using ShareLoader.Models;
using ShareLoader.Share;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShareLoader.Background;

public class MoveChecker
{
    public DateTime LastChecked { get; set; } = DateTime.Now;
    private DownloadItem? _currentItem;
    private DownloadType _currentType;
    private string _currentSort = "";
    private List<DownloadItem> _items = new();
    DownloadChecker download;
    ExtractChecker extract;

    public MoveChecker(DownloadChecker down, ExtractChecker ex)
    {
        download = down;
        extract = ex;
        Check();
    }

    private async void Check()
    {
        while(true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if(SettingsHelper.GetSetting<SettingsModel>("settings") == null || 
                download.isDownloading || !download.nothingToDownload || 
                extract.isExtracting || !extract.nothingToExtract) continue;

            _currentItem = null;
            Dictionary<int, string> sorts = new Dictionary<int, string>();
            Dictionary<int, DownloadType> types = new Dictionary<int, DownloadType>();
            Dictionary<int, IEnumerable<IGrouping<int, DownloadItem>>> groups = new Dictionary<int, IEnumerable<IGrouping<int, DownloadItem>>>();
            using(DownloadContext context = new DownloadContext())
            {
                foreach(DownloadGroup group in context.Groups)
                {
                    groups.Add(group.Id, context.Items.Where(i => i.DownloadGroupID == group.Id).ToList().GroupBy(i => i.GroupID));
                    types.Add(group.Id, group.Type);
                    sorts.Add(group.Id, group.Sort);
                }
            }
            
            foreach(int groupId in groups.Keys)
            {
                var x = groups[groupId];
                foreach(var igroup in x)
                {
                    int count = igroup.Count();
                    if(count == igroup.Count(i => i.State == States.Extracted))
                    {
                        _currentItem = igroup.ElementAt(0);
                        _currentType = types[_currentItem.DownloadGroupID];
                        _currentSort = sorts[_currentItem.DownloadGroupID];
                        _items = igroup.ToList();
                        break;
                    }
                }
                if(_currentItem != null) break;
            }


            if(_currentItem != null)
                DoMove();

            LastChecked = DateTime.Now;
        }
    }

    private async void DoMove()
    {
        if(_currentItem == null) return;
        System.Console.WriteLine("Moving now: " + _currentItem.Name);
        ChangeItemState(States.Moving);
        await SocketHelper.Instance.SendIDMoving(_currentItem);

        switch(_currentType)
        {
            case DownloadType.Movie:
                MoveMovie();
                break;
            case DownloadType.Soap:
                MoveSoap();
                break;
            case DownloadType.Other:
                MoveOther();
                break;
        }
        
        ChangeItemState(States.Finished);
        _ = SocketHelper.Instance.SendIDFinish(_currentItem);
        System.Console.WriteLine("Moving finished");
    }

    private void MoveMovie()
    {
        SettingsModel? settings = SettingsHelper.GetSetting<SettingsModel>("settings");
        if(settings == null || _currentItem == null) return;
        string pathFrom = System.IO.Path.Combine(settings.DownloadFolder, _currentItem.DownloadGroupID.ToString(), "extracted", _currentItem.GroupID.ToString());
        string[] files = Directory.GetFiles(pathFrom, "*.mkv");
        string fileToMove = "";

        foreach (string file in files)
        {
            if (Path.GetFileName(file).Contains("sample"))
                continue;

            fileToMove = file;
            break;
        }

        string pathTo = Path.Combine(settings.MoveFolder, "Filme");
        string fileTo = Path.Combine(pathTo, _currentSort + Path.GetExtension(fileToMove));

        if (!Directory.Exists(pathTo))
            Directory.CreateDirectory(pathTo);
        
        try
        {
            File.Move(fileToMove, fileTo, true);
        } catch(Exception e)
        {
            Console.WriteLine("Verschieben: " + e.Message);
        }
    }

    private void MoveSoap()
    {
        bool dyn = _currentSort.EndsWith("dynamisch");

        
        if (dyn)
            _currentSort = _currentSort.Substring(0, _currentSort.LastIndexOf("/"));
        else
            _currentSort = _currentSort.Replace('/', Path.DirectorySeparatorChar);
        
        SettingsModel? settings = SettingsHelper.GetSetting<SettingsModel>("settings");

        if(settings == null || _currentItem == null) return;
        string pathFrom = System.IO.Path.Combine(settings.DownloadFolder, _currentItem.DownloadGroupID.ToString(), "extracted", _currentItem.GroupID.ToString());
        string[] files = Directory.GetFiles(pathFrom, "*.mkv");

        foreach(string file in files)
        {

            string filename = Path.GetFileName(file);
            if (filename.Contains("sample"))
                continue;

            string pathTo = "";

            if(dyn)
            {
                Regex reg = new Regex(@"(?i)s([0-9]{1,2})[\.]{0,1}e[0-9]{1,2}");
                Match m = reg.Match(filename);
                pathTo = Path.Combine(settings.MoveFolder, "Serien", _currentSort, "Staffel " + m.Groups[1].Value);
            } else
            {
                pathTo = Path.Combine(settings.MoveFolder, "Serien", _currentSort);
            }

            if(!Directory.Exists(pathTo))
                Directory.CreateDirectory(pathTo);

            string endpath = Path.Combine(pathTo, filename);

            try
            {
                File.Move(file, endpath, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Verschieben: " + e.Message);
            }
        }
    }

    public void MoveOther()
    {
        SettingsModel? settings = SettingsHelper.GetSetting<SettingsModel>("settings");
        if(settings == null || _currentItem == null) return;
        string pathFrom = System.IO.Path.Combine(settings.DownloadFolder, _currentItem.DownloadGroupID.ToString(), "extracted", _currentItem.GroupID.ToString());
        string[] files = Directory.GetFiles(pathFrom);
        string pathTo = Path.Combine(settings.MoveFolder, "Other");
        if(!string.IsNullOrEmpty(_currentSort))
        {
            pathTo = Path.Combine(pathTo, _currentSort);
        }

        if (!Directory.Exists(pathTo))
            Directory.CreateDirectory(pathTo);

        foreach (string file in files)
        {
            string fileTo = Path.Combine(pathTo, Path.GetFileName(file));
            File.Move(file, fileTo, true);
        }
    }

    
    private void ChangeItemState(States state)
    {
        using(DownloadContext context = new DownloadContext())
        {
            foreach(DownloadItem item in _items)
            {   
                item.State = state;
                context.Items.Update(item);
            }
            context.SaveChanges();
        }
    }
}