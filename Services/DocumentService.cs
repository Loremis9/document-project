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
        private UserService userService;

        public DocumentService(DocumentationDbContext context, RigthAccessService rigthAccessService,
        LuceneSearchService luceneService, DocumentFilesService documentationFileService, AIService aiService, UserService userService)
        {
            _context = context;
            this.rigthAccessService = rigthAccessService;
            this.luceneService = luceneService;
            this.documentationFileService = documentationFileService;
            this.aiService = aiService;
            this.userService = userService;

        }

        public async Task<Documentation> CreateDocument(string name, string description, bool isPublic, int userId, string path, string tags)
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("Utilisateur non trouvé");
            }

            var document = new Documentation
            {
                Title = name,
                Description = description,
                IsPublic = isPublic,
                RootPath = path,
                Tags = tags
            };


            await _context.Documentations.AddAsync(document);
            await _context.SaveChangesAsync(); // Ici, document.Id est rempli par EF Core

            await rigthAccessService.AddFirstUserToDocumentation(userId, document.Id);

            luceneService.IndexDocument(document);

            return document;
        }

        public async Task<OutputDocument> ImportDocument(int userId, string path, string title, string description, bool isPublic, string tags)
        {
            try
            {
                // Crée le document
                var document = await CreateDocument(title, description, isPublic, userId, path, tags);

                // Scan fichiers
                var tree = SampleUtils.GetDirectoryTree(path, includeFiles: true);
                var docs = ScannerUtils.ScanDirectory(path);

                // Crée les entrées pour chaque fichier
                foreach (var doc in docs)
                {

                    var documentFile = await documentationFileService.CreateDocumentFile(document.Id, doc.Path, doc.IsDirectory, userId);
                    if (!doc.IsDirectory)
                    {
                        var content = await File.ReadAllTextAsync(doc.Path); // async
                    }
                }

                // Index le document global après succès complet
                luceneService.IndexDocument(document);

                return new OutputDocument
                {
                    Id = document.Id,
                    Title = document.Title,
                    Tags = document.Tags,
                    Tree = tree
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de l'import du document : {ex.Message}", ex);
            }
        }

        public async Task<Documentation?> FindDocumentById(int id, int userId)
        {
            return await _context.Documentations.FirstOrDefaultAsync(d => d.Id == id);
        }


        public async Task<Documentation?> FindDocumentByName(string name, int userId)
        {
            return await _context.Documentations.FirstOrDefaultAsync(d => d.Title == name);
        }


        public async Task<string> SearchByPrompt(int userId, string prompt, string model)
        {
            // Récupère les permissions utilisateur
            var userPermissions = await rigthAccessService.GetAllUserPermission(userId);

            // Reformulation et extraction des tags via IA avec fallback
            //série de plusieurs mot clé pour chercher dans une documentation
            var q = await aiService.AskQuestionToAi(userId, prompt, "reformule", SampleUtils.GenerateUUID(), model, null) ?? prompt;
            //tags court 3 pour selectionner par tags
            var tagsRaw = await aiService.AskAi(userId, prompt, "tag", SampleUtils.GenerateUUID()) ?? "";
            Console.WriteLine($" reformule : {q} \n tag: {tagsRaw}");
            // Nettoyage basique des tags (si IA retourne une chaîne)
            tagsRaw = tagsRaw.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();

            var conversationId = SampleUtils.GenerateUUID();
            var documentation = new List<(int DocId, string Snippet)>();
            // Agrège les résultats Lucene pour toutes les permissions
            foreach (var userPermission in userPermissions)
            {
                var docs = luceneService.SearchWithHighlights(q, userPermission.DocumentationId, tagsRaw);
                documentation.AddRange(docs);
            }

            if (!documentation.Any())
                return "Aucun document trouvé pour votre recherche.";

            // Prépare les chunks pour l’IA
            var responses = new StringBuilder();
            var chunkedDocs = SampleUtils.PrepareChunksForOllama(documentation);

            foreach (var chunk in chunkedDocs)
            {
                Console.WriteLine($"Chunk: {chunk}");

                var chunkResponse = await aiService.AskAi(userId, chunk, "search", conversationId);
                if (!string.IsNullOrEmpty(chunkResponse))
                {
                    responses.AppendLine(chunkResponse);
                }
            }

            return responses.ToString();
        }

    }
}
