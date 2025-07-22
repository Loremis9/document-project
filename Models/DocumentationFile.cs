namespace WEBAPI_m1IL_1.Models
{
    public class DocumentationFile
    {
        public int Id { get; set; }
        public int DocumentationId { get; set; }
        public Documentation Documentation { get; set; }
        public string Tags { get; set; }
        public string FullPath { get; set; }
        public bool IsFolder { get; set; }
    }
}
