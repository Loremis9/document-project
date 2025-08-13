using WEBAPI_m1IL_1.Models;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Utils;
using WEBAPI_m1IL_1.DTO;
using System.Runtime.CompilerServices;
using WEBAPI_m1IL_1.Helpers;

namespace WEBAPI_m1IL_1.Services
{
    public class DocumentFilesService
    {
        private RigthAccessService rigthAccessService;
        private DocumentationDbContext _context;
        private LuceneSearchService luceneService;
        private AIService aiService;
        private MinIoService _minIoService;
        private ConvertToMarkdownService _convertToMarkdownService;
        public DocumentFilesService(DocumentationDbContext context, RigthAccessService rigthAccessService, LuceneSearchService luceneService, AIService aiService, MinIoService minIoService, ConvertToMarkdownService convertToMarkdownService)
        {
            _context = context;
            this.rigthAccessService = rigthAccessService;
            this.luceneService = luceneService;
            this.aiService = aiService;
            _minIoService = minIoService;
            _convertToMarkdownService = convertToMarkdownService;
        }

        public async Task<DocumentationFile> CreateDocumentFile(int documentId, string path, bool isFolder, int userId, Stream fileStream, string ext)
        {
            var contentMarkDown = "";
            try
            {
                var contextRequest = SampleUtils.GenerateUUID();
                var documentFile = new DocumentationFile();
                if(isFolder){
                 documentFile.IsFolder = isFolder;
                 documentFile.DocumentationId = documentId;
                 documentFile.Tags = "folder";
                 documentFile.Description = "folder";
                 documentFile.FullPath = path;
                 var pathFile =  await _minIoService.CreateDirectory(path);
                 documentFile.FullPath = pathFile;
                }
                else
                {
                    if(FilesUtils.IsImage(ext))
                    {
                        using var ms = new MemoryStream();
                        await fileStream.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();
                        var images = JsonHelper.ExtractMetadata(await aiService.AskDescriptionImageToAi(System.Text.Encoding.UTF8.GetString(fileBytes), SampleUtils.GenerateUUID()));
                        documentFile.IsFolder = isFolder;
                        documentFile.DocumentationId = documentId;
                        documentFile.Tags = string.Join(",",images.Tags);
                        documentFile.Description = images.Description;
                        documentFile.FullPath = path;
                        var image = await _minIoService.UploadImageAsync(documentFile, fileBytes);
                        documentFile.FullPath = image;
                    }
                    else
                    {
                    
                        var content = await _convertToMarkdownService.ExtractFromFile(fileStream, documentId,ext,path);
                        // Tags générés par l'IA
                        var tags = await aiService.AskAi(userId, content, "tag", SampleUtils.GenerateUUID());

                        // Découpe et conversion en parallèle
                        var chunks = SampleUtils.ChunkString(content, 10000);
                        var convertTasks = chunks.Select(chunk => aiService.AskAi(userId, chunk, "convert", contextRequest));
                        var convertedChunks = await Task.WhenAll(convertTasks);
                        //chunck aussi les decription et ensuite demander à l'ai de synthétiser
                        var description = await aiService.AskAi(userId, content, "description", contextRequest);
                        contentMarkDown = string.Join("\n", convertedChunks);
                        var markdownPath = Path.ChangeExtension(path, ".md");
                        documentFile.FullPath = path;
                        documentFile.IsFolder = isFolder;
                        documentFile.DocumentationId = documentId;
                        documentFile.Tags = tags;
                        documentFile.Description = description;
                        var pathFile =  await  _minIoService.UploadDocumentFileAsync(documentFile, contentMarkDown);
                        documentFile.FullPath = pathFile;
                    }
                }

                // Ajout en DB
                await _context.DocumentationFiles.AddAsync(documentFile);
                await _context.SaveChangesAsync();

                if (!FilesUtils.IsImage(ext))
                { 
                    luceneService.IndexDocumentFile(documentFile, contentMarkDown);
                }

                return documentFile;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la création du fichier : {ex.Message}", ex);
            }
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
