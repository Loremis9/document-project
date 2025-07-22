using WEBAPI_m1IL_1.Models;

namespace WEBAPI_m1IL_1.Services
{
    public class DocumentFilesService
    {

        private DocumentationDbContext _context;
        public DocumentFilesService(DocumentationDbContext context) {
            _context = context;
        }
        public void CreateDocumentFile(int documentId, string path)
        {
            // j'ouvre le fichier 
            // passer à chatgpt
            // l'enregistrer 
            // prendre le lien et mettre à jour le documentfiles
        }
        public void DeleteDocumentFIle(int documentId)
        {

        }

        public void ModifyDocumentFiles(int documentId , string path, string content)
        {

        }

        public async Task<DocumentationFile> FindByFolderName(string name)
        {
            return _context.DocumentationFiles
                .FirstOrDefault(df => df.FullPath == name && df.IsFolder);
        }
        public async Task<DocumentationFile> FindByDocumentId(int documentId)
        {
            return _context.DocumentationFiles
                .FirstOrDefault(df => df.DocumentationId == documentId);
        }
    }
}
