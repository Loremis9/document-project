using WEBAPI_m1IL_1.Models;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Utils;
using WEBAPI_m1IL_1.DTO;
using System.Runtime.CompilerServices;
namespace WEBAPI_m1IL_1.Services
{
    public class DocumentFilesService
    {
        private RigthAccessService rigthAccessService;
        private DocumentationDbContext _context;
        private LuceneSearchService luceneService;
        private AIService aiService;
        public DocumentFilesService(DocumentationDbContext context, RigthAccessService rigthAccessService, LuceneSearchService luceneService, AIService aiService)
        {
            _context = context;
            this.rigthAccessService = rigthAccessService;
            this.luceneService = luceneService;
            this.aiService = aiService;
        }

        public async Task<DocumentationFile> CreateDocumentFile(int documentId, string path, bool isFolder, int userId)
        {
            try
            {
                var contextRequest = SampleUtils.GenerateUUID();
                var documentFile = new DocumentationFile();
                if (FilesUtils.IsImage(path))
                {
                    var description = await aiService.AskDescriptionImageToAi(userId, SampleUtils.ConvertImageToBase64(path), contextRequest);
                    var tags = await aiService.AskAi(userId, description, "tag", contextRequest);
                    documentFile.FullPath = path;
                    documentFile.IsFolder = isFolder;
                    documentFile.DocumentationId = documentId;
                    documentFile.Tags = tags;
                    documentFile.description = description;
                }
                else
                {
                    var content = await TransformDocumentFileToText(path);
                    // Tags générés par l'IA
                    var tags = await aiService.AskAi(userId, content, "tag", SampleUtils.GenerateUUID());

                    // Découpe et conversion en parallèle
                    var chunks = SampleUtils.ChunkString(content, 10000);
                    var convertTasks = chunks.Select(chunk => aiService.AskAi(userId, chunk, "convert", contextRequest));
                    var convertedChunks = await Task.WhenAll(convertTasks);
                    //chunck aussi les decription et ensuite demander à l'ai de synthétiser
                    var description = await aiService.AskAi(userId, content, "description", contextRequest);
                    var contentMarkDown = string.Join("\n", convertedChunks);
                    var markdownPath = Path.ChangeExtension(path, ".md");
                    await File.WriteAllTextAsync(markdownPath, contentMarkDown);
                    documentFile.FullPath = path;
                    documentFile.IsFolder = isFolder;
                    documentFile.DocumentationId = documentId;
                    documentFile.Tags = tags;
                    documentFile.description = description;
                }
                // Ajout en DB
                await _context.DocumentationFiles.AddAsync(documentFile);
                await _context.SaveChangesAsync();

                luceneService.IndexDocumentFile(documentFile, path);

                return documentFile;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la création du fichier : {ex.Message}", ex);
            }
        }

        public async Task<string> TransformDocumentFileToText(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            string convertToText = FilesUtils.ExtractFromFile(filePath, filePath + "/image");

            return convertToText;
        }


        public async Task DeleteDocumentFile(int documentId, int userId)
        {
            var permission = await rigthAccessService.HavePermissionTo(userId, documentId, "delete");
            if (permission)
            {
                var files = await _context.DocumentationFiles
                    .Where(df => df.DocumentationId == documentId)
                    .ToListAsync();

                _context.DocumentationFiles.RemoveRange(files);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new UnauthorizedAccessException($"User {userId} does not have read permission for documentation {documentId}.");
            }

        }

        public async Task ModifyDocumentFiles(int userId, int documentId, string path, string content)
        {
            var file = await _context.DocumentationFiles
                .FirstOrDefaultAsync(df => df.DocumentationId == documentId && df.FullPath == path);
            var permission = await rigthAccessService.HavePermissionTo(userId, documentId, "write");
            if (permission)
            {
                if (file != null)
                {
                    FilesUtils.OverwriteFile(file.FullPath, content);
                }
            }
            else
            {
                throw new UnauthorizedAccessException($"User {userId} does not have read permission for documentation {documentId}.");
            }
        }

        public async Task<List<DocumentationFile>> FindByFoldersName(int userId, string name, int documentId)
        {
            var permission = await rigthAccessService.HavePermissionTo(userId, documentId, "read");
            if (!permission)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have read permission for documentation {documentId}.");
            }
            return await _context.DocumentationFiles
                .Where(df => df.FullPath == name && df.IsFolder && df.DocumentationId == documentId)
                .ToListAsync();
        }


        public async Task<DocumentationFile> FindDocumentFileByDocumentIdAndDocumentFileId(int userId, int documentId, int documentFileId)
        {
            var permission = await rigthAccessService.HavePermissionTo(userId, documentId, "read");
            if (!permission)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have read permission for documentation {documentId}.");
            }
            return await _context.DocumentationFiles
                .FirstOrDefaultAsync(df => df.DocumentationId == documentId && df.Id == documentFileId);
        }

        public async Task<List<DocumentationFile>> FindDocumentFileByDocumentFileId(int userId, int documentId, int documentFileId)
        {
            var permission = await rigthAccessService.HavePermissionTo(userId, documentId, "read");
            if (!permission)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have read permission for documentation {documentId}.");
            }
            return await _context.DocumentationFiles
                .Where(df => df.DocumentationId == documentId)
                .ToListAsync();
        }
        public async Task<List<DocumentationFile>> GetAllFilesByDocumentId(int userId, int documentId)
        {
            var permission = await rigthAccessService.HavePermissionTo(userId, documentId, "read");
            if (!permission)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have read permission for documentation {documentId}.");
            }
            return await _context.DocumentationFiles
                .Where(df => df.DocumentationId == documentId)
                .ToListAsync();
        }

        public async Task<List<OutputDocumentFile>> GetByTagAsync(string tag, int userId)
        {
            // Récupère les permissions de l'utilisateur
            var permissions = await rigthAccessService.GetAllUserPermission(userId);

            if (permissions == null)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have permissions to access any documentation.");
            }

            // Liste des DocumentationId accessibles
            var allowedIds = permissions.Select(p => p.DocumentationId).ToList();

            var docsMetadata = await _context.DocumentationFiles
                .Where(d => d.Tags != null
                            && d.Tags.Contains(tag)
                            && allowedIds.Contains(d.DocumentationId))
                .ToListAsync();

            var documents = new List<OutputDocumentFile>();

            foreach (var d in docsMetadata)
            {
                documents.Add(new OutputDocumentFile
                {
                    Id = d.Id,
                    DocumentationId = d.DocumentationId,
                    Tags = d.Tags.Split(',').ToList(),
                    FileContent = await FilesUtils.ReadFileAsync(d.FullPath) // Lecture réelle
                });
            }

            return documents;
        }
    }
}
