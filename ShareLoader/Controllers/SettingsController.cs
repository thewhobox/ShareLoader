using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Data;
using ShareLoader.Models;

namespace ShareLoader.Controllers;

public class SettingsController : Controller
{
    private readonly DownloadContext _context;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(DownloadContext context, ILogger<SettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
}
