using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataRetrieval.DbProvider;
using Microsoft.AspNetCore.Mvc;
using DataRetrieval.Models;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Npgsql;

namespace DataRetrieval.Controllers
{
    public class HomeController : Controller
    {
        private readonly PostgreSqlDbProvider dbProvider;
        public HomeController(PostgreSqlDbProvider dbProvider)
        {
            this.dbProvider = dbProvider;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public IActionResult Lab1()
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
            try
            {
                var pattern = @".+\((\d{4})\)";
                var yearFinder = new Regex(pattern);
                var match = yearFinder.Match(query);
                var year = 0;

                query = query.Replace("%", @"\%").Replace("_", @"\_");

                if (match.Success)
                {
                    int.TryParse(match.Groups[1].Value, out year);

                    query = Regex.Replace(query, @"\((\d{4})\)", "");
                }
                var yearCondition = year == 0 ? "" : $"OR year = {year}";
                var result = await dbProvider.GetRowsAsync(condition: $"name ~~* '%{query}%' {yearCondition}", count: 10);

                return Json(result);
            }
            catch (System.Exception e)
            {
                return Json(new {e.Message, e.StackTrace});
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}