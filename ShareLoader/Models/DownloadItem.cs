using ShareLoader.Classes;
using ShareLoader.Share;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShareLoader.Models;

public class DownloadItem
{
    [Key]
    public int Id { get; set; } = 0;
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public int DownloadGroupID { get; set; } = 0;
    public int GroupID { get; set; } = -1;
    [MaxLength(150)]
    public string Url { get; set; } = "";
    public long Size { get; set; } = 0;
    public States State { get; set; } = States.Waiting;
    [MaxLength(32)]
    public string MD5 { get; set; } = "";      
    public string Hoster { get; set; } = "";


    public DownloadItem() { }

    public DownloadItem(ItemModel model)
    {
        ItemId = model.Id;
        Name = model.Name;
        GroupID = model.GroupId;
        Size = model.Size;
        Hoster = model.Downloader;
        Url = model.Url;
    }


    public string GetSize()
    {
        return DownloadHelper.GetSizeString(Size);
    }

    public string GetImage()
    {
        switch (State)
        {
            case States.Finished:
                return "task_alt";

            case States.Downloaded:
                return "file_download_done";

            default:
            case States.Waiting:
                return "hourglass_bottom";

            case States.Downloading:
                return "downloading";

            case States.Error:
                return "warning_amber";

            case States.Extracting:
                return "folder_zip";

            case States.Extracted:
                return "folder";

            case States.Moving:
                return "copy_all";

            case States.Paused:
                return "pause";
        }
    }
    
    public string GetColor()
    {
        switch (State)
        {
            case States.Finished:
                return "green";

            case States.Downloaded:
                return "orange darken-2";

            default:
            case States.Waiting:
                return "gray darken-1";

            case States.Downloading:
                return "orange lighten-2";

            case States.Error:
                return "red";

            case States.Extracting:
                return "purple lighten-2";

            case States.Extracted:
                return "purple darken-2";

            case States.Moving:
                return "brown lighten-1";
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
    Error,
    Paused
}