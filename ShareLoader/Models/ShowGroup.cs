using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class ShowGroup
    {
        public DownloadGroup Group { get; set; }
        public List<DownloadItem> Items { get; set; }
    }
}
