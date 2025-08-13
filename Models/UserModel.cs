using WEBAPI_m1IL_1.Helpers;
namespace WEBAPI_m1IL_1.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        private string _password;
        public string Password
        {
            get => _password;
            set => _password = PasswordHelper.HashPassword(value);
        }

        // Permissions directes sur les documentations
        public ICollection<UserPermission> UserPermissions { get; set; }
        
        // Propriété pour vérifier si l'utilisateur est admin global
        public bool IsGlobalAdmin { get; set; } = false;
    }
}
