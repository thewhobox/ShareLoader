using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class DownloadSettings
    {
        [Display(Name = "Sofort entpacken")]
        public bool ExtractImmedialy { get; set; }

        [Display(Name = "Sofort verschieben")]
        public bool MoveImmedialy { get; set; }

        [Display(Name = "Download-Ordner")]
        public string DownloadFolder { get; set; }

        [Display(Name = "Move-Ordner")]
        public string MoveFolder { get; set; }

        [Display(Name = "Log-Ordner")]
        public string LogFolder { get; set; }

        [Display(Name = "Pfad zum Entpacker")]
        public string ZipPath { get; set; }

        [Display(Name = "Entpacken Parameter")]
        public string ZipArgs { get; set; }

        [Display(Name = "Download Intervall")]
        public int IntervalDownload { get; set; }

        [Display(Name = "Entpacken Intervall")]
        public int IntervalExtract { get; set; }

        [Display(Name = "Verschieben Intervall")]
        public int IntervalMove { get; set; }


#if DEBUG
        [JsonIgnore]
        private static string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.dev.json");
#else
        [JsonIgnore]
        private static string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.base.json");
#endif


        public static DownloadSettings Load()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<DownloadSettings>(System.IO.File.ReadAllText(path));
        }

        public void Save()
        {
            try
            {
                string x = Newtonsoft.Json.JsonConvert.SerializeObject(this);
                System.IO.File.WriteAllText(path, x);
            } catch(Exception e)
            {

            }
        }
    }
}
