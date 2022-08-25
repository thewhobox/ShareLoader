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

    public IActionResult Add()
    {
        _context.Accounts.Add(new Account() { Name = "ddownload", Username = "nutzer82683", Password = "123456789", Hoster = "ddl" });
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Delete(int Id)
    {
        Account acc = _context.Accounts.SingleOrDefault(g => g.Id == Id);
        if(acc == null) return NotFound();
        _context.Accounts.Remove(acc);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
}
