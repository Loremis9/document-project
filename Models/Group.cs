namespace WEBAPI_m1IL_1.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<UserGroup> UserGroups { get; set; }
        public ICollection<DocumentationGroup> DocumentationGroups { get; set; }
    }
}
