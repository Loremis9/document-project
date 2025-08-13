 namespace WEBAPI_m1IL_1.Models
{
    public class UserPermission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UserModel User { get; set; }
        
        public int DocumentationId { get; set; }
        public Documentation Documentation { get; set; }
        
        // Permissions
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
        public bool IsAdmin { get; set; }
        
        // Métadonnées
        public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    }
}