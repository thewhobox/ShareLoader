using System.ComponentModel.DataAnnotations;

namespace ShareLoader.Models;

public class Account
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public float TrafficLeft { get; set; }
    public float TrafficLeftWeek { get; set; }
    public bool IsPremium { get; set; }
    public DateTime ValidTill { get; set; }
    public float Credit { get; set; }
    public string Hoster { get; set; }
    public bool AllowClientRedirect { get; set; } = true;
}