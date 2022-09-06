namespace ShareLoader.Models;

public class SettingsModel
{
    public string DownloadFolder { get; set; } = "/share";
    public string MoveFolder { get; set; } = "/share/out";
    public string LogFolder { get; set; } = "/share";
    public string ZipPath { get; set; } = "7z";
    public string ZipArgs { get; set; } = "e \"%filesDir%\" -bsp1 -aoa -o\"%extractDir%\" -p%pass%";
    public int MaxDownloads { get; set; } = 1;
}