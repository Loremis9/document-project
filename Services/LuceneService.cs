using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Classic;
using WEBAPI_m1IL_1.Models;
using Lucene.Net.Store;
using Lucene.Net.Search.Highlight;
using WEBAPI_m1IL_1.Utils;
using System.IO;
namespace WEBAPI_m1IL_1.Services
{
    public class LuceneSearchService
    {
        private readonly Lucene.Net.Store.Directory _directory;
        private readonly StandardAnalyzer _analyzer;
        private readonly string _indexPath;
        private readonly LuceneVersion _version = LuceneVersion.LUCENE_48;
        const int maxChunkSize = 5000; // caractères max par chunk

        public LuceneSearchService()
        {
            _indexPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "LuceneIndex");
            System.IO.Directory.CreateDirectory(_indexPath);
            _directory = Lucene.Net.Store.FSDirectory.Open(_indexPath);
            _analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        }

        public void IndexDocument(Documentation doc)
        {
            using var writer = new IndexWriter(_directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, _analyzer));
            var document = new Document
        {
            new StringField("Id", doc.Id.ToString(), Field.Store.YES),
            new TextField("Title", doc.Title ?? "", Field.Store.YES),
            new TextField("Description", doc.Description ?? "", Field.Store.YES),
            new TextField("Tags", doc.Tags ?? "", Field.Store.YES),
            new StringField("IsPublic", doc.IsPublic.ToString().ToLower(), Field.Store.YES)

        };

            writer.AddDocument(document);
            writer.Commit();
        }
        public void IndexDocumentFile(DocumentationFile documentFile, string content)
        {
            var indexConfig = new IndexWriterConfig(_version, _analyzer);
            using var writer = new IndexWriter(_directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, _analyzer));

            // Supprimer les anciens chunks liés à ce docId
            writer.DeleteDocuments(new Term("documentFileId", documentFile.Id.ToString()));
            var chunkNumber = 0;
            foreach (var chunk in SampleUtils.ChunkString(content, maxChunkSize))
            {
                var doc = new Document
                    {
                        new StringField("documentId", documentFile.DocumentationId.ToString(), Field.Store.YES),
                        new StringField("documentFileId", documentFile.Id.ToString(), Field.Store.YES),
                        new Int32Field("ChunkNumber", chunkNumber, Field.Store.YES),
                        new TextField("Content", chunk, Field.Store.YES)
                    };
                if (!string.IsNullOrWhiteSpace(documentFile.Tags))
                {
                    var tagsArray = documentFile.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var tag in tagsArray)
                    {
                        doc.Add(new StringField("Tag", tag.Trim().ToLower(), Field.Store.YES));
                    }
                }
                writer.AddDocument(doc);
                chunkNumber++;
            }
            writer.Flush(triggerMerge: false, applyAllDeletes: false);
        }


        public List<(int DocId, string Snippet)> SearchWithHighlights(string queryText, int documentationId, string? tags)
        {
            Console.WriteLine($"Searching with query: {queryText}, DocumentationId: {documentationId}, Tags: {tags}");
            int maxResults = 20;
            var keywords = queryText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!keywords.Any())
                return new List<(int, string)>();

            using var reader = DirectoryReader.Open(_directory);
            var searcher = new IndexSearcher(reader);
            var booleanQuery = BuildQueryFromKeywords(keywords, documentationId, tags);
            var hits = searcher.Search(booleanQuery, maxResults).ScoreDocs;
            var results = new List<(int, string)>();
            var query = new BooleanQuery();
            foreach (var kw in keywords)
                query.Add(new TermQuery(new Term("Content", kw)), Occur.SHOULD);
            var parser = new QueryParser(_version, "Content", _analyzer);

            var scorer = new QueryScorer(query);
            var formatter = new SimpleHTMLFormatter("<b>", "</b>");
            var highlighter = new Highlighter(formatter, scorer)
            {
                TextFragmenter = new SimpleFragmenter(100)
            };

            foreach (var hit in hits)
            {
                var doc = searcher.Doc(hit.Doc);
                int id = int.Parse(doc.Get("Id"));
                string content = doc.Get("Content");

                using var tokenStream = _analyzer.GetTokenStream("Content", content);
                string fragment = highlighter.GetBestFragment(tokenStream, content);

                if (string.IsNullOrEmpty(fragment))
                {
                    fragment = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                }

                results.Add((id, fragment));
            }

            return results;
        }
        private BooleanQuery BuildQueryFromKeywords(string[] keywords, int documentationId, string? tags)
        {
            var booleanQuery = new BooleanQuery();

            // Ajouter les mots-clés avec OR
            var keywordQuery = new BooleanQuery();
            foreach (var keyword in keywords)
            {
                keywordQuery.Add(new TermQuery(new Term("Content", keyword.ToLowerInvariant())), Occur.SHOULD);
            }
            booleanQuery.Add(keywordQuery, Occur.MUST);

            // Filtre par DocumentationId
            booleanQuery.Add(new TermQuery(new Term("DocumentationId", documentationId.ToString())), Occur.MUST);

            // Filtre par isPublic
            booleanQuery.Add(new TermQuery(new Term("isPublic", "False")), Occur.MUST);

            // Filtre par tags (si nécessaire)
            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagsArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (tagsArray.Any())
                {
                    var tagsQuery = new BooleanQuery();
                    foreach (var tag in tagsArray)
                    {
                        tagsQuery.Add(new TermQuery(new Term("Tag", tag.Trim().ToLower())), Occur.SHOULD);
                    }
                    booleanQuery.Add(tagsQuery, Occur.MUST);
                }
            }

            return booleanQuery;
        }
    }
}