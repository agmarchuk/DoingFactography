using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyFamilyFactografy.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyFamilyFactografy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(string id)
        {
            return View("Index", id);
        }

        public IActionResult Person(string id)
        {
            RDFEngine.RRecord rr = Infobase.engine.GetRRecord(id);
            return View("Person", rr);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
