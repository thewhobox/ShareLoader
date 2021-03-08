using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class DownloadGroup
    {
        public int ID { get; set; }
        [MaxLength(200)]
        public string Name { get; set; }
        [MaxLength(100)]
        public string Password { get; set; }
        public DownloadType Type { get; set; }
        public DownloadTarget Target { get; set; }
        public string Sort { get; set; }
        public bool IsTemp { get; set; } = false;

        [NotMapped]
        public List<DownloadItem> Items { get; set; } = new List<DownloadItem>();
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

    public enum DownloadTarget
    {
        [Display(Name = "Beide")]
        Both,
        Alex,
        Mike
    }
}
