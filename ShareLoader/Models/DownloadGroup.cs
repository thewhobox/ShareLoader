using System;
using System.Collections.Generic;
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