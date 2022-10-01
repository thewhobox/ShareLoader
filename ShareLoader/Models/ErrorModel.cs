using System.ComponentModel.DataAnnotations;

namespace ShareLoader.Models;

public class ErrorModel
{
    [Key]
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int ItemId { get; set; }
    public string FileName { get; set; }
    public string Text { get; set; } = "";
}
