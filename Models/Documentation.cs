namespace WEBAPI_m1IL_1.Models
{
    public class Documentation
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Tags { get; set; }
        public string RootPath { get; set; }
        public bool IsPublic { get; set; }
        public ICollection<DocumentationGroup> DocumentationGroups { get; set; }
        public ICollection<DocumentationFile> DocumentationFiles { get; set; }
    }
}
