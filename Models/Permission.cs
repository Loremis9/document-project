namespace WEBAPI_m1IL_1.Models
{
    public class Permission
    {
        public int DocumentationId { get; set; }
        public int GroupId { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
        public bool isAdmin { get; set; }
        public DocumentationGroup DocumentationGroup { get; set; }
    }
}
