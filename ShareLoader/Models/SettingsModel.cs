namespace ShareLoader.Models;

public class SettingsModel
{
    public string DownloadFolder { get; set; } = "/shareloader";
    public string MoveFolder { get; set; } = "/emby";
    public string LogFolder { get; set; } = "/shareloader";
    public string ZipPath { get; set; } = "7z";
    public string ZipArgs { get; set; } = "e \"%filesDir%\" -bsp1 -aoa -o\"%extractDir%\" -p%pass%";
    public int MaxDownloads { get; set; } = 3;
}