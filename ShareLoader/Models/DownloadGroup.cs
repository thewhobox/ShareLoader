using System;
using ShareLoader.Share;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareLoader.Models;

public class DownloadGroup
{
    [Key]
    public int Id { get; set; }
    [MaxLength(200)]
    public string Name { get; set; } = "";
    [MaxLength(100)]
    public string Password { get; set; } = "";
    public DownloadType Type { get; set; }
    public string Sort { get; set; } = "";
    public int ItemsCount { get; set; }

    public DownloadGroup() { }

    public DownloadGroup(CheckViewModel model)
    {
        Name = model.Name;
        Password = model.Password ?? "";
        Sort = model.NameToSort;
        ItemsCount = model.Links.Count;
        Type = model.Type;
    }
}