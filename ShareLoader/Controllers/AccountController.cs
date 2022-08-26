using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShareLoader.Data;
using ShareLoader.Models;

namespace ShareLoader.Controllers;

public class AccountsController : Controller
{
    private readonly DownloadContext _context;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(DownloadContext context, ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(_context.Accounts.ToList());
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(AccountModel acc)
    {
        _context.Accounts.Add(acc);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Delete(int Id)
    {
        AccountModel acc = _context.Accounts.SingleOrDefault(g => g.Id == Id);
        if(acc == null) return NotFound();
        _context.Accounts.Remove(acc);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
}
