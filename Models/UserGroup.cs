namespace WEBAPI_m1IL_1.Models
{
    public class UserGroup
    {
        public int UserId { get; set; }
        public UserModel User { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
}
