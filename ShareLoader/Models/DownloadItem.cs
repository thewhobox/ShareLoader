using ShareLoader.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Models
{
    public class DownloadItem
    {
        public enum States
        {
            Waiting,
            Downloading,
            Downloaded,
            Extracting,
            Extracted,
            Moving,
            Finished,
            Error
        }

        [Key]
        public int ID { get; set; }
        public int DownloadGroupID { get; set; }
        public int GroupID { get; set; } = -1;
        [MaxLength(15)]
        public string ShareId { get; set; }
        [MaxLength(150)]
        public string Url { get; set; }
        public int Size { get; set; }
        [MaxLength(200)]
        public string Name { get; set; }
        public States State { get; set; } = States.Waiting;
        [MaxLength(32)]
        public string MD5 { get; set; }
        [MaxLength(3)]
        public string Hoster { get; set; }

        private int _perc = 0;

        public void SetPercentage(int perc)
        {
            _perc = perc;
        }

        public string GetClass()
        {
            return MD5 == "error" ? "alert" : "";
        }

        public string GetAttributes()
        {
            string check = MD5 == "error" ? "" : " checked";
            string disabled = MD5 == "error" ? " disabled " : "";
            return disabled + check;
        }

        public string GetSize()
        {
            double size = Size;
            string[] format = new string[] { "{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB" };
            int i = 0;
            while (i < format.Length && size >= 1024)
            {
                size = (long)(100 * size / 1024) / 100.0;
                i++;
            }
            return string.Format(format[i], size);
        }

        public string GetImage()
        {
            switch (State)
            {
                case States.Finished:
                    return "mif-checkmark fg-green";

                case States.Downloaded:
                    return "mif-checkmark fg-orange";

                default:
                case States.Waiting:
                    return "mif-hour-glass fg-darkGrey";

                case States.Downloading:
                    return "mif-file-download fg-darkGrey";

                case States.Error:
                    return "mif-warning fg-red";

                case States.Extracting:
                    return "mif-stackoverflow fg-mauve ani-flash";

                case States.Extracted:
                    return "mif-stackoverflow fg-darkMauve";

                case States.Moving:
                    return "mif-file-download fg-darkBlue ani-flash";
            }
        }

        public string GetContentView()
        {
            switch (State)
            {
                case States.Finished:
                    return defaultProgress("green");

                case States.Downloaded:
                    return defaultProgress("brown");


                case States.Downloading:
                    return defaultProgress("green", _perc);

                default:
                case States.Waiting:
                    return defaultProgress("green", 0); // "<div class='mt-1' data-role='progress' data-small='true' data-animate='500' data-role='progress'></div>";

                case States.Error:
                    return defaultProgress("red");

                case States.Extracting:
                    return defaultProgress("mauve");

                case States.Extracted:
                    return defaultProgress("darkMauve");

                case States.Moving:
                    return defaultProgress("darkBlue");
            }
        }


        private string defaultProgress(string color, int value = 100)
        {
            return "<div class='mt-1' data-role='progress' data-small='true' data-animate='500' data-role='progress' data-value='" +value + "' data-cls-bar='bg-" + color + "'></div>";
        }
        
    }
}
