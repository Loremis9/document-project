using WEBAPI_m1IL_1.Models;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Utils;

namespace WEBAPI_m1IL_1.Services
{
    public class DocumentFilesService
    {
        private RigthAccessService rigthAccessService;
        private DocumentationDbContext _context;
        private LuceneSearchService luceneService;
        private AIService aiService;
        public DocumentFilesService(DocumentationDbContext context,RigthAccessService rigthAccessService ) {
            _context = context;
            this.rigthAccessService = rigthAccessService;
            this.luceneService = luceneService;
            this.aiService = aiService;
        }
        
        public async Task<DocumentationFile> CreateDocumentFile(int documentId, string path, bool isFolder,int userId)
        {
            var content = FilesUtils.ReadFile(path);
            var tags = await aiService.AskAi(userId,content,"tag",null,null);
            var contentMarkDown = new List<string>();
            var chunks = SampleUtils.ChunkString(content, 12000);

            foreach (var chunk in chunks)
            {
                var response = await aiService.AskAi(userId, chunk, "convert", null, null);
                contentMarkDown.Add(response);
            }
            var documentFile = new DocumentationFile {
                DocumentationId = documentId,
                Tags = tags,
                FullPath = path,
                IsFolder = isFolder
            };

            luceneService.IndexDocumentFile(documentFile,path);
            _context.DocumentationFiles.Add(documentFile);
            await _context.SaveChangesAsync();
            return documentFile;
        }
        
        public async Task DeleteDocumentFile(int documentId,int userId)
        {
            var permission = await rigthAccessService.HavePermissionTo(userId,documentId,"delete");
            if (permission){
            var files = await _context.DocumentationFiles
                .Where(df => df.DocumentationId == documentId)
                .ToListAsync();
                
            _context.DocumentationFiles.RemoveRange(files);
            await _context.SaveChangesAsync();
            }

        }

        public async Task ModifyDocumentFiles(int userId,int documentId, string path, string content)
        {
            var file = await _context.DocumentationFiles
                .FirstOrDefaultAsync(df => df.DocumentationId == documentId && df.FullPath == path);
            var permission = await rigthAccessService.HavePermissionTo(userId,documentId,"write");
            if(permission)
            {
                if (file != null)
                {
                    FilesUtils.OverwriteFile(file.FullPath,content);
                }
            }
        }

        public async Task<List<DocumentationFile>> FindByFoldersName(string name, int documentId)
        {
            return await _context.DocumentationFiles
                .Where(df => df.FullPath == name && df.IsFolder && df.DocumentationId == documentId)
                .ToListAsync();
        }
        
        
        public async Task<DocumentationFile> FindDocumentFileByDocumentIdAndDocumentFileId(int documentId, int documentFileId)
        {
            return await _context.DocumentationFiles
                .FirstOrDefaultAsync(df => df.DocumentationId == documentId && df.Id == documentFileId);
        }

                public async Task<List<DocumentationFile>> FindDocumentFileByDocumentFileId(int documentId, int documentFileId)
        {
            return await _context.DocumentationFiles
                .Where(df => df.DocumentationId == documentId)
                .ToListAsync();
        }
        public async Task<List<DocumentationFile>> GetAllFilesByDocumentId(int documentId)
        {
            return await _context.DocumentationFiles
                .Where(df => df.DocumentationId == documentId)
                .ToListAsync();
        }
    }
}
