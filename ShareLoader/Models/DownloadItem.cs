using ShareLoader.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareLoader.Models;

public class DownloadItem
{
    [Key]
    public int Id { get; set; }
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public int DownloadGroupID { get; set; }
    public int GroupID { get; set; } = -1;
    [MaxLength(150)]
    public string Url { get; set; }
    public int Size { get; set; }
    public States State { get; set; } = States.Waiting;
    [MaxLength(32)]
    public string MD5 { get; set; }        
    public string Hoster { get; set; }



    public string GetSize()
    {
        return DownloadHelper.GetSizeString(Size);
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
}


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