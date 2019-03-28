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

        private static string indexLocation = System.IO.Directory.GetCurrentDirectory();
        private static FSDirectory dir = FSDirectory.Open(indexLocation);
        private static LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;
        private static StandardAnalyzer analyzer = new StandardAnalyzer(luceneVersion);
        private static IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
        private static IndexWriter writer1 = new IndexWriter(dir, indexConfig);


        public HomeController(PostgreSqlDbProvider dbProvider)
        {
            this.dbProvider = dbProvider;
            InitLucy();
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
            var counter = 0;
            var searchQuery = query.ToLower();
            var array = searchQuery.Split(' ').ToList();
            var searcher = new IndexSearcher(writer1.GetReader(applyAllDeletes: true));

            var totalResults = new List<Document>();
            //одно слово
            var phrase = new MultiPhraseQuery();
            foreach (var word in array)
            {
                phrase = new MultiPhraseQuery();
                if (!string.IsNullOrEmpty(word))
                {
                    phrase.Add(new Term("name_word", word));
                    var res = searcher.Search(phrase, 10).ScoreDocs;
                    foreach (var hit in res)
                    {
                        var foundDoc = searcher.Doc(hit.Doc);
                        if (!totalResults.Any(f =>
                            f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
                            totalResults.Add(foundDoc);
                    }
                }
            }
//
//            // полное название
//            phrase = new MultiPhraseQuery();
//            phrase.Add(new Term("full_name", query));
//            var hits = searcher.Search(phrase, 10).ScoreDocs;
//            foreach (var hit in hits)
//            {
//                var foundDoc = searcher.Doc(hit.Doc);
//                if (!totalResults.Any(f => f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
//                    totalResults.Add(foundDoc);
//            }
//
//            //части слов
//            foreach (var word in array)
//            {
//                if (!string.IsNullOrEmpty(word))
//                {
//                    var wild = new WildcardQuery(new Term("name_word", "*" + word + "*"));
//                    var res = searcher.Search(wild, 10).ScoreDocs;
//                    foreach (var hit in res)
//                    {
//                        var foundDoc = searcher.Doc(hit.Doc);
//                        if (!totalResults.Any(f =>
//                            f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
//                            totalResults.Add(foundDoc);
//                    }
//                }
//            }
//
//            //год и часть слова
//            var year_to_find = "";
//            var number = 0;
//            foreach (var word in array)
//            {
//                var result = TryParse(word, out number);
//                if (result && number > 1800 && number <= 9999)
//                {
//                    year_to_find = word;
//                    array.RemoveAt(array.IndexOf(word));
//                    break;
//                }
//            }
//
//            Console.WriteLine(number != 0);
//
//            if (number != 0)
//            {
//                phrase = new MultiPhraseQuery();
//                foreach (var word in array)
//                {
//                    if (!string.IsNullOrEmpty(word))
//                    {
//                        var booleanQuery = new BooleanQuery();
//
//                        var wild = new WildcardQuery(new Term("name_word", "*" + word + "*"));
//                        var num = NumericRangeQuery.NewInt32Range("year", 1, number, number, true, true);
//
//                        booleanQuery.Add(wild, Occur.SHOULD);
//                        booleanQuery.Add(num, Occur.SHOULD);
//                        var res = searcher.Search(booleanQuery, 10).ScoreDocs;
//                        foreach (var hit in res)
//                        {
//                            var foundDoc = searcher.Doc(hit.Doc);
//                            if (!totalResults.Any(f =>
//                                f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
//                                totalResults.Add(foundDoc);
//                        }
//                    }
//                }
//            }


            foreach (var doc in totalResults)
            {
                yield return (doc.GetValues("full_name")[0], (int) doc.GetField("year").GetInt32Value());
            }
        }

        public async Task InitLucy()
        {
            var found = false;
            using (var conn = new NpgsqlConnection("Server=84.201.147.162; Port=5432; User Id=developer; Password=rtfP@ssw0rd; Database=CoderLiQ"))
            {
                conn.Open();

                var cmd = new NpgsqlCommand("SELECT * FROM movies", conn);
                var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    found = true;
                    Console.WriteLine("connection established");
                }

                if (found == false)
                {
                    Console.WriteLine("Data does not exist");
                }

                dr.Close();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        var sourceId = reader.GetInt32(0);
                        var sourceName = reader.GetString(1);
                        int.TryParse(reader.GetInt16(2).ToString(), out var yearInt);

                        var doc = new Document();
                        doc.Add(new Field("full_name", sourceName, StringField.TYPE_STORED));
                        //var word = source.name.Split(' ')[0];
                        foreach (var word in sourceName.Split(' '))
                        {
                            if (!string.IsNullOrEmpty(word))
                                doc.Add(new Field("name_word", word, TextField.TYPE_STORED));
                        }

                        doc.Add(new StoredField("id", sourceId));
                        doc.Add(new Int32Field("year", yearInt, Field.Store.YES));
                        writer1.AddDocument(doc);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}