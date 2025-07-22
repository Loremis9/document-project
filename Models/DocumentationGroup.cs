namespace WEBAPI_m1IL_1.Models
{
    public class DocumentationGroup
    {
        public int DocumentationId { get; set; }
        public Documentation Documentation { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public Permission Permission { get; set; }
    }
}
