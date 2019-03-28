using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Npgsql;

namespace DataRetrieval
{
    public class LucyAdapter
    {
        private static string indexLocation = System.IO.Directory.GetCurrentDirectory() + "/indexes";
        private static FSDirectory dir = FSDirectory.Open(indexLocation);
        private static LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;
        private static StandardAnalyzer analyzer = new StandardAnalyzer(luceneVersion);
        private static IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
        public IndexWriter lucyWriter = new IndexWriter(dir, indexConfig);
        private static bool isInitialized;

        public void InitLucy()
        {
            if (!isInitialized)
                using (var conn = new NpgsqlConnection("Host=84.201.147.162;Port=5432;Database=CoderLiQ;Username=developer;Password=rtfP@ssw0rd"))
                {
                    conn.Open();

                    var cmd = new NpgsqlCommand("SELECT * FROM movies", conn);
                    var dr = cmd.ExecuteReader();
                    dr.Close();
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var sourceId = reader.GetInt32(2);
                        var sourceName = reader.GetString(1);
                        int.TryParse(reader.GetInt16(0).ToString(), out var yearInt);

                        var doc = new Document {new Field("full_name", sourceName, StringField.TYPE_STORED)};
                        foreach (var word in sourceName.Split(' '))
                        {
                            if (!string.IsNullOrEmpty(word))
                                doc.Add(new Field("name_word", word, TextField.TYPE_STORED));
                        }

                        doc.Add(new StoredField("id", sourceId));
                        doc.Add(new Int32Field("year", yearInt, Field.Store.YES));
                        lucyWriter.AddDocument(doc);
                    }
                }

            isInitialized = true;
        }
    }
}