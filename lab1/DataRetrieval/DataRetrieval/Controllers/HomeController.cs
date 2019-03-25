using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataRetrieval.DbProvider;
using Microsoft.AspNetCore.Mvc;
using DataRetrieval.Models;
using Microsoft.EntityFrameworkCore;

namespace DataRetrieval.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly PostgreSqlDbProvider dbProvider;

        public HomeController(ApplicationDbContext context, PostgreSqlDbProvider dbProvider)
        {
            this.context = context;
            this.dbProvider = dbProvider;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> TestGetAllRowsFromMovies()
        {
            var x = await dbProvider.GetRowsAsync("movies");

            return Json(x);
        }


        public async Task<IActionResult> Search(string query)
        {
            var yearFinder = new Regex(@".+\((\d{4})\)");
            var match = yearFinder.Match(query);
            var year = 0;

            if (match.Success)
            {
                year = int.Parse(match.Groups[1].Value);
            }

            var result = context.movies.Where(e => e.name.Contains(query) && year == default(int) || e.year == year);

            return Json(await result.Take(10).ToListAsync());
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}