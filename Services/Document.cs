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
        public void AddFileToDocument(int documentId, string path)
        {
            //trouver le document
            // trouver le dossier ou crée un nouveau dossier 
            //crée le documentFile
        }

        public void ImportDocument(bool isPublic, string name, string description, string tags)
        {

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

        public async Task<Documentation> CreateDocument(string name, string description, string tags, bool isPublic)
        {
            var document = new Documentation
            {
                Title = name,
                Description = description,
                Tags = tags,
                IsPublic = isPublic
            };
            _context.Documentations.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<List<int>> GetDocumentIdsByGroupIds(List<int> groupIds)
        {
            var docIds = _context.DocumentationGroups
                .Where(gd => groupIds.Contains(gd.GroupId))
                .Select(gd => gd.Documentation.Id)
                .Distinct()
                .ToList();
            return docIds;
        }
    }
}
