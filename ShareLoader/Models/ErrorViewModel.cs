using System;
using System.Collections.Generic;

namespace ShareLoader.Models
{
    public class ErrorViewModel
    {
        public DownloadGroup Group { get; set; }
        public int ItemId { get; set; }

        public List<DownloadError> Errors { get; set; }
    }
}