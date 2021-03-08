using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Models;

namespace ShareLoader.Controllers
{
    public class SettingsController : Controller
    {
        private readonly DownloadContext _context;
        private readonly DownloadSettings _settings;

        public SettingsController(DownloadContext context)
        {
            _context = context;
            _settings = DownloadSettings.Load(); ;
        }
        
        public IActionResult Index()
        {
            return View(_settings);
        }

        [HttpPost]
        public IActionResult Index(DownloadSettings settings)
        {
            settings.Save();
            return View(_settings);
        }


        public IActionResult Accounts()
        {
            List<AccountModel> model = _context.Accounts.ToList();
            return View(model);
        }
        
        [Route("/Settings/Account/Create")]
        public IActionResult AccountCreate()
        {
            ViewData["hoster"] = DownloadHelper.GetAllHosterNames();
            return View();
        }

        [HttpPost]
        [Route("/Settings/Account/Create")]
        public IActionResult AccountCreate(AccountModel acc)
        {
            Downloader.IDownloader downloader = DownloadHelper.GetDownloader(acc.Hoster);
            if (downloader.IsFree)
                acc.IsPremium = true;

            _context.Accounts.Add(acc);
            _context.SaveChanges();
            return RedirectToAction("Accounts");
        }


        [Route("/Settings/Account/Delete/{id}")]
        public IActionResult AccountDelete(int id)
        {
            AccountModel acc = _context.Accounts.Single(a => a.ID == id);
            _context.Accounts.Remove(acc);
            _context.SaveChanges();
            return RedirectToAction("Accounts");
        }


        [Route("/Settings/Account/Edit/{id}")]
        public IActionResult AccountEdit(int id)
        {
            if (!_context.Accounts.Any(a => a.ID == id))
                return NotFound();

            return View(_context.Accounts.Single(a => a.ID == id));
        }

        [HttpPost]
        [Route("/Settings/Account/Edit/{id}")]
        public IActionResult AccountEdite(AccountModel acc)
        {
            Downloader.IDownloader downloader = DownloadHelper.GetDownloader(acc.Hoster);
            if (downloader.IsFree)
                acc.IsPremium = true;

            _context.Accounts.Add(acc);
            _context.SaveChanges();
            return RedirectToAction("Accounts");
        }

    }
}