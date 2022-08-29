namespace ShareLoader.CNL.Models;

using System.ComponentModel.DataAnnotations;

public class CheckViewModel
{
    public string Name { get; set; }
    public string Password { get; set; }
    public List<LinkGroup> Groups { get; set; }
    public DownloadType Type { get; set; }
    public string RawLinks { get; set; }
    public string Search { get; set; }
    public int RawLinksCount { get; set; }
}

public class LinkGroup
{
    public string Name { get; set; }
    public string[] Links { get; set; }
}


public enum DownloadType
{
    [Display(Name = "Unbekannt")]
    Unknown,
    [Display(Name = "Film")]
    Movie,
    [Display(Name = "Serie")]
    Soap,
    [Display(Name = "Anderes")]
    Other
}