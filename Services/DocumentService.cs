using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.FindB;
using WEBAPI_m1IL_1.Models;
using WEBAPI_m1IL_1.Utils;
using WEBAPI_m1IL_1.DTO;
using System.Text;

namespace WEBAPI_m1IL_1.Services
{
    public class DocumentService
    {
        private readonly DocumentationDbContext _context;
        private RigthAccessService rigthAccessService;
        private LuceneSearchService luceneService;
        private DocumentFilesService documentationFileService;
        private AIService aiService;
        public DocumentService(DocumentationDbContext context,RigthAccessService rigthAccessService,
        LuceneSearchService luceneService,DocumentFilesService documentationFileService,AIService aiService)
        {
            _context = context;
            this.rigthAccessService = rigthAccessService;
            this.luceneService = luceneService;
            this.documentationFileService = documentationFileService;
            this.aiService = aiService;;
        }

        public async Task<Documentation> CreateDocument(string name, string description, bool isPublic, int userid,string path)
        {
            var document = new Documentation
            {
                Title = name,
                Description = description,
                IsPublic = isPublic,
                RootPath = path
            };
             _context.Documentations.Add(document);
            var documentation = await _context.SaveChangesAsync();
            rigthAccessService.AddFirstUserToDocumentation(userid,documentation);
            await _context.SaveChangesAsync();
            luceneService.IndexDocument(document);

            return document;
        }

        public async Task<OutputDocument> ImportDocument(int userId,string path,string title, string description,bool isPublic)
        {
            var document = await CreateDocument(title,description,isPublic,userId,path);
            luceneService.IndexDocument(document);
            var docs = ScannerUtils.ScanDirectory(path);
            foreach (var doc in docs)
            {
                var documentFile = await documentationFileService.CreateDocumentFile(document.Id,doc.Path,doc.IsDirectory,userId);
                luceneService.IndexDocumentFile(documentFile,File.ReadAllText(doc.Path));
            }
            var tree = FilesUtils.GetDirectoryTree(path,includeFiles: true);
            return new OutputDocument { Id = document.Id,Title = document.Title,Tags=document.Tags,Tree=tree };
        }

        public async Task<Documentation?> FindDocumentById(int id)
        {
            return await _context.Documentations.FirstOrDefaultAsync(d => d.Id == id);
        }


        public async Task<Documentation?> FindDocumentByName(string name)
        {
            return await _context.Documentations.FirstOrDefaultAsync(d => d.Title == name);
        }

        public async Task<List<Documentation>> GetByTagAsync(string tag)
        {
            return await _context.Documentations
                .Where(d => d.Tags != null && d.Tags.Contains(tag))
                .ToListAsync();
        }

        public async Task<string> SearchByPrompt(int userId,string prompt){

            var userPermissions = await rigthAccessService.GetAllUserPermission(userId);
            var q = await aiService.AskAi(userId,null,"reformule", prompt, SampleUtils.GenerateUUID());
            var tags = await aiService.AskAi(userId,null,"tags", prompt,SampleUtils.GenerateUUID());
            var conversationId = SampleUtils.GenerateUUID();
            var documentation = new List<(int DocId, string Snippet)>();

            foreach(var userPermission in userPermissions){
                documentation = luceneService.SearchWithHighlights(q,userPermission.DocumentationId,tags);
            }
            var responses = new StringBuilder();
            var chunkedDocs = SampleUtils.PrepareChunksForOllama(documentation);
            foreach (var chunk in chunkedDocs)
            {
                var chunkResponse = aiService.AskAi(userId,chunk,"search", null, conversationId);

                if (chunkResponse != null)
                {
                    responses.Append(chunkResponse);
                }
            }

            // Concatène toutes les réponses en une seule string
            return string.Join(" ", responses);
        }

    }
}
