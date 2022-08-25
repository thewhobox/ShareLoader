using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Data;
using ShareLoader.Models;

namespace ShareLoader.Controllers;

public class DownloadsController : Controller
{
    private readonly DownloadContext _context;
    private readonly ILogger<DownloadsController> _logger;

    public DownloadsController(DownloadContext context, ILogger<DownloadsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(_context.Groups.ToList());
    }

    public IActionResult Add()
    {
        _context.Groups.Add(new DownloadGroup() { Name = "Testxy", ItemsCount = 120 });
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
    

    public IActionResult Delete(int Id)
    {
        DownloadGroup group = _context.Groups.SingleOrDefault(g => g.Id == Id);
        if(group == null) return NotFound();
        _context.Groups.Remove(group);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
}
