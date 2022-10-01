using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using ShareLoader.Models;
using ShareLoader.Share;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShareLoader.Background;

public class ExtractChecker
{
    public DateTime LastChecked { get; set; } = DateTime.Now;
    public bool isExtracting = false;
    public bool nothingToExtract = false;
    private DownloadItem? _currentItem;
    private List<DownloadItem> _items;
    private CancellationTokenSource _extractToken;
    private string _currentPassword;
    DownloadChecker download;

    public ExtractChecker(DownloadChecker down)
    {
        download = down;
        Check();
    }

    private async void Check()
    {
        while(true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if(SettingsHelper.GetSetting<SettingsModel>("settings") == null || 
                download.isDownloading || !download.nothingToDownload ||isExtracting) continue;

            _currentItem = null;
            Dictionary<int, string> passwords = new Dictionary<int, string>();
            Dictionary<int, IEnumerable<IGrouping<int, DownloadItem>>> groups = new Dictionary<int, IEnumerable<IGrouping<int, DownloadItem>>>();
            using(DownloadContext context = new DownloadContext())
            {
                foreach(DownloadGroup group in context.Groups)
                {
                    groups.Add(group.Id, context.Items.Where(i => i.DownloadGroupID == group.Id).ToList().GroupBy(i => i.GroupID));
                    passwords.Add(group.Id, group.Password);
                }
            }
            
            foreach(int groupId in groups.Keys)
            {
                var x = groups[groupId];
                foreach(var igroup in x)
                {
                    int count = igroup.Count();
                    if(count == igroup.Count(i => i.State == States.Downloaded))
                    {
                        if(igroup.Count() > 1)
                        {
                            Regex reg = new Regex(@"\.part([0]{0,})1\.rar");
                            _currentItem = igroup.Single(i => reg.IsMatch(i.Name));
                        } else {
                            _currentItem = igroup.ElementAt(0);
                        }
                        _currentPassword = passwords[_currentItem.DownloadGroupID];
                        _items = igroup.ToList();
                        break;
                    }
                }
                if(_currentItem != null) break;
            }

            if(_currentItem != null)
            {
                nothingToExtract = false;
                DoExtract();
            }
            else
                nothingToExtract = true;

            LastChecked = DateTime.Now;
        }
    }

    private async void DoExtract()
    {
        System.Console.WriteLine("Extracting now: " + _currentItem.Name);
        ChangeItemState(States.Extracting);
        _ = SocketHelper.Instance.SendIDExtract(_currentItem);
        _extractToken = new CancellationTokenSource();

        SettingsModel settings = SettingsHelper.GetSetting<SettingsModel>("settings");
        string fileDir = Path.Combine(settings.DownloadFolder, _currentItem.DownloadGroupID.ToString(), "files", _currentItem.Name);
        string extractDir = Path.Combine(settings.DownloadFolder, _currentItem.DownloadGroupID.ToString(), "extracted", _currentItem.GroupID.ToString());
        string args = settings.ZipArgs.Replace("%filesDir%", fileDir).Replace("%extractDir%", extractDir).Replace("%pass%", _currentPassword);

        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo.FileName = settings.ZipPath;
        p.StartInfo.Arguments = args;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.Start();

        GetProgress(p);

        await p.WaitForExitAsync(_extractToken.Token);

        if(_extractToken.IsCancellationRequested)
        {
            p.Close();
        }

        if(p.ExitCode != 0 || foundError || _extractToken.IsCancellationRequested)
        {
            string msg = $"ExitCode: {p.ExitCode}\r\n{errortext}";
            ChangeItemState(States.Error, msg);
            _ = SocketHelper.Instance.SendIDError(_currentItem, msg);
            _currentItem = null;
            return;
        }

        try
        {
            foreach (DownloadItem item in _items)
            {
                string dfolder = settings.DownloadFolder;
                string gid = _currentItem.DownloadGroupID.ToString();
                string name = item.Name;
                fileDir = Path.Combine(dfolder, gid, "files", name);
                System.IO.File.Delete(fileDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        ChangeItemState(States.Extracted);
        _ = SocketHelper.Instance.SendIDExtracted(_currentItem);
        _currentItem = null;
    }

    public async Task StopExtract(DownloadItem item)
    {
        if(_currentItem == null || _currentItem.Id != item.Id || _extractToken == null) return;
        _extractToken.Cancel();

        while(_currentItem != null)
            await Task.Delay(5);
    }

    private bool foundError = false;
    string errortext = "";

    private async void GetProgress(System.Diagnostics.Process p)
    {
        foundError = false;
        errortext = "";
        try{
            Regex reg = new Regex("^[ ]{0,2}([0-9]{1,3})%");
            char[] buffer = new char[1024];
            string currentLine = "";


            _= System.Threading.Tasks.Task.Run(async () => {
                while (!p.HasExited)
                {

                    
                    int readed = p.StandardOutput.Read(buffer, 0, 1024);

                    foreach (char c in buffer)
                    {
                        if (c != '\0' && c != '\n')
                        {
                            if (c == '\r')
                            {
                                Match m = reg.Match(currentLine);

                                if (m.Success)
                                {
                                    if(m.Groups[1].Value != "0")
                                        await SocketHelper.Instance.SendIDExtract(_currentItem, int.Parse(m.Groups[1].Value));
                                }

                                currentLine = "";
                            }
                            else
                            {
                                currentLine = currentLine + c.ToString();
                            }
                        }
                    }
                }
            });

            _= System.Threading.Tasks.Task.Run(() => {
                while (!p.HasExited)
                {
                    string? error = p.StandardError.ReadLine();
                    if(string.IsNullOrEmpty(error) || error.Length < 5) continue;
                    errortext += error + "\r\n";
                    foundError = true;
                }
            });
            
        } catch (Exception ex)
        {
            System.Console.WriteLine("Es trat ein Fehler auf: " + ex.Message);
        }
        while (!p.HasExited)
        {
            await Task.Delay(10);
        }
    }

    private void ChangeItemState(States state, string message = "")
    {
        using(DownloadContext context = new DownloadContext())
        {
            foreach(DownloadItem item in _items)
            {   
                item.State = state;
                context.Items.Update(item);
            }

            if(state == States.Error && _currentItem != null)
            {
                context.Errors.Add(new ErrorModel() { GroupId = _currentItem.DownloadGroupID, ItemId = _currentItem.Id, Text = message, FileName = _currentItem.Name });
            }
            context.SaveChanges();
        }
    }
}