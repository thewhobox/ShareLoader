using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class DownloadError
    {
        public int ID { get; set; }
        public int SourceId { get; set; }
        public int ItemID { get; set; }
        public string Error { get; set; }
        [MaxLength(200)]
        public string Message { get; set; }
        public DateTime Time { get; set; }

        public DownloadError() { }

        public DownloadError(int id, DownloadItem item, Exception e)
        {
            try
            {
                Error = Newtonsoft.Json.JsonConvert.SerializeObject(e);
            } catch { }
            ItemID = item.ID;
            Message = e.Message;
            Time = DateTime.Now;
            SourceId = id;
        }

        public Exception GetException()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Exception>(Error);
        }
    }
}