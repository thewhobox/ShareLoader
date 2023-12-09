using System.ComponentModel.DataAnnotations;
using ShareLoader.Classes;

namespace ShareLoader.Models;

public class AccountModel
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public float TrafficLeft { get; set; }
    public float TrafficLeftWeek { get; set; }
    public bool IsPremium { get; set; }
    public DateTime ValidTill { get; set; }
    public float Credit { get; set; }
    public string Hoster { get; set; } = "";
    public bool AllowClientRedirect { get; set; } = true; //Todo remove

    public bool IsValid()
    {
        return ValidTill > DateTime.Now;
    }

    
    public string GetSize()
    {
        return DownloadHelper.GetSizeString(TrafficLeft);
    }

}