using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DataRetrieval.Models;
using Microsoft.AspNetCore.Http;

namespace DataRetrieval.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext context;

        public HomeController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View();
//            await context.AddAsync(new Movie {name = "Green Elephant", year = 1337 });
//            await context.SaveChangesAsync();
//            return Json(context.movies.Count());
        }

        
        /// <summary>
        /// Не говнокод, а скоростное программирование
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            try
            {
                var rawData = await ReadAsListAsync(file);

                rawData.RemoveAt(0);

                var movies = new List<Movie>();
                foreach (var line in rawData)
                {
                    try
                    {
                        var arr = line.Split(",");
                        var str = arr[1].Trim();
                        movies.Add(new Movie
                        {
                            id = Convert.ToInt32(arr[0].TrimStart('0')),
                            name = str.Substring(0, (str.Length - 6)).Trim(),
                            year = Convert.ToInt32(str.Substring(str.Length - 5, 4))
                        });
                    }
                    catch (Exception e)
                    {
                        // потеряем пару битых строк - пофиг
                    }
                    
                }

                await context.AddRangeAsync(movies.Take(10));
                await context.SaveChangesAsync();
                return Json(movies.Count);
            }
            catch (Exception e)
            {
                return Json(new {e.Message, e.StackTrace});
            }
        }

        public static async Task<List<string>> ReadAsListAsync(IFormFile file)
        {
            var result = new List<string>();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.Add(await reader.ReadLineAsync());
            }
            return result;
        }
        public static async Task<string> ReadAsStringAsync(IFormFile file)
        {
            var result = new StringBuilder();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(await reader.ReadLineAsync());
            }
            return result.ToString();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}