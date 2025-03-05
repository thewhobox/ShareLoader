namespace ShareLoader.Share;

using System.ComponentModel.DataAnnotations;

public class CheckViewModel
{
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
    public List<ItemModel> Links { get; set; } = new List<ItemModel>();
    public DownloadType Type { get; set; }
    public string RawLinks { get; set; } = "";
    public string Search { get; set; } = "";
    public string NameToSort { get; set; } = "";
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