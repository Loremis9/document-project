namespace WEBAPI_m1IL_1.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public string Password { get; set; }
        
        // Permissions directes sur les documentations
        public ICollection<UserPermission> UserPermissions { get; set; }
        
        // Propriété pour vérifier si l'utilisateur est admin global
        public bool IsGlobalAdmin { get; set; } = false;
    }
    

    
    public class UserConstants
    {
        public static List<UserModel> Users = new List<UserModel>()
        {
            new UserModel() { Username = "jason_admin", EmailAddress = "jason.admin@email.com", Password = "MyPass_w0rd", IsGlobalAdmin = true },
            new UserModel() { Username = "elyse_seller", EmailAddress = "elyse.seller@email.com", Password = "MyPass_w0rd" },
        };
    }
}
