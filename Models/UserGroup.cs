using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WEBAPI_m1IL_1.Models
{
    [Index(nameof(UserId), IsUnique = true)]

    public class UserGroup
    {
        public int UserId { get; set; }
        public UserModel User { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
}
