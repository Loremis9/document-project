using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.FindB;
using WEBAPI_m1IL_1.Models;

namespace WEBAPI_m1IL_1.Services
{
    public class Document
    {
        private readonly DocumentationDbContext _context;

        public Document(DocumentationDbContext context)
        {
            _context = context;
        }
        public void AddFileToDocument()
        {
           //trouver le document
           // trouver le dossier ou crée un nouveau dossier 
           //crée le documentFile
        }

        public void ImportDocument()
        {

        }

        public async Task<Documentation?> FindDocumentById(int id)
        {
            return await _context.Documentations.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Documentation>> GetByTagAsync(string tag)
        {
            return await _context.Documentations
                .Where(d => d.Tags != null && d.Tags.Contains(tag))
                .ToListAsync();
        }


    }
}
