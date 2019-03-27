using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DataRetrieval.Controllers
{
    public class DataController : Controller
    {
        private readonly ApplicationDbContext context;

        public DataController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Не говнокод, а скоростное программирование (хотя, учитывая, что даже проверки на дубликаты нет...)
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
                    catch (Exception)
                    {
                        // потеряем пару битых строк - пофиг
                    }

                }

                await context.AddRangeAsync(movies);
                await context.SaveChangesAsync();
                return Json(movies.Count);
            }
            catch (Exception e)
            {
                return Json(new { e.Message, e.StackTrace });
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
    }
}