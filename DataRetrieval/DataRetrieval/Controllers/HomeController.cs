using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataRetrieval.DbProvider;
using Microsoft.AspNetCore.Mvc;
using DataRetrieval.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;

namespace DataRetrieval.Controllers
{
    public class HomeController : Controller
    {
        private readonly PostgreSqlDbProvider dbProvider;

        private readonly LucyAdapter lucyAdapter;

        public HomeController(PostgreSqlDbProvider dbProvider, LucyAdapter lucyAdapter)
        {
            this.dbProvider = dbProvider;
            this.lucyAdapter = lucyAdapter;
            lucyAdapter.InitLucy();
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
                var result =
                    await dbProvider.GetRowsAsync(condition: $"name ~~* '%{query}%' {yearCondition}", count: 10);

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

        public IEnumerable<(string name, int year)> SearchWithLucy(string query)
        {
            var words = query.Split(' ').ToList();
            var searcher = new IndexSearcher(lucyAdapter.lucyWriter.GetReader(applyAllDeletes: true));

            var totalResults = new List<Document>();
            //word
            MultiPhraseQuery multiPhraseQuery;
            foreach (var word in words)
            {
                multiPhraseQuery = new MultiPhraseQuery();
                if (string.IsNullOrEmpty(word)) continue;
                multiPhraseQuery.Add(new Term("name_word", word));
                var docs = searcher.Search(multiPhraseQuery, 10).ScoreDocs;
                foreach (var doc in docs)
                {
                    var document = searcher.Doc(doc.Doc);
                    if (totalResults.All(f => f.GetField("id").GetInt32Value() != document.GetField("id").GetInt32Value()))
                        totalResults.Add(document);
                }
            }

            // full name
            multiPhraseQuery = new MultiPhraseQuery();
            multiPhraseQuery.Add(new Term("full_name", query));
            var scoreDocs = searcher.Search(multiPhraseQuery, 10).ScoreDocs;
            foreach (var scoreDoc in scoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                if (totalResults.All(f => f.GetField("id").GetInt32Value() != doc.GetField("id").GetInt32Value()))
                    totalResults.Add(doc);
            }

            //word parts
            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(word)) continue;
                var wildcardQuery = new WildcardQuery(new Term("name_word", "*" + word + "*"));
                var docs = searcher.Search(wildcardQuery, 10).ScoreDocs;
                foreach (var doc in docs)
                {
                    var document = searcher.Doc(doc.Doc);
                    if (totalResults.All(f => f.GetField("id").GetInt32Value() != document.GetField("id").GetInt32Value()))
                        totalResults.Add(document);
                }
            }

            //year and word part
            var number = 0;
            foreach (var word in words)
            {
                var result = int.TryParse(word, out number);
                if (!result) continue;
                words.RemoveAt(words.IndexOf(word));
                break;
            }

            if (number != 0)
            {
                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word)) continue;
                    var booleanQuery = new BooleanQuery();

                    var wildcardQuery = new WildcardQuery(new Term("name_word", "*" + word + "*"));
                    var rangeQuery = NumericRangeQuery.NewInt32Range("year", 1, number, number, true, true);

                    booleanQuery.Add(wildcardQuery, Occur.SHOULD);
                    booleanQuery.Add(rangeQuery, Occur.SHOULD);
                    var docs = searcher.Search(booleanQuery, 10).ScoreDocs;
                    foreach (var doc in docs)
                    {
                        var foundDoc = searcher.Doc(doc.Doc);
                        if (totalResults.All(f => f.GetField("id").GetInt32Value() != foundDoc.GetField("id").GetInt32Value()))
                            totalResults.Add(foundDoc);
                    }
                }
            }

            foreach (var doc in totalResults)
            {
                yield return (doc.GetValues("full_name")[0], (int) doc.GetField("year").GetInt32Value());
            }
        }
    }
}