namespace ShareLoader.Share;

public class ItemModel
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Downloader { get; set; }
    public long Size { get; set; }
    public bool IsOnline { get; set; }
    public int GroupId { get; set; }
    public string Url { get; set; }
}