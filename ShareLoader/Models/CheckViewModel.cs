namespace ShareLoader.Models;

public class CheckViewModel
{
    public string Name { get; set; }
    public string Password { get; set; }
    public List<LinkGroup> Groups { get; set; }
    public DownloadType Type { get; set; }
    public string NameToSort { get; set; }
    public string RawLinks { get; set; }
    public string Search { get; set; }
}

public class LinkGroup
{
    public string Name { get; set; }
    public string[] Links { get; set; }
}