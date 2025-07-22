using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Models;
using WEBAPI_m1IL_1.Utils;
namespace WEBAPI_m1IL_1.Services
{
    public class RigthAccessService
    {
        private readonly DocumentationDbContext _context;

        public RigthAccessService(DocumentationDbContext context)
        {
            _context = context;
        }
        public bool HasAccess(string userId, string resourceId)
        {
            // Logique pour vérifier si l'utilisateur a accès à la ressource
            // Par exemple, vérifier dans une base de données ou un service externe
            // Pour l'instant, on retourne toujours true pour simuler un accès autorisé
            return true;
        }

        public void ModifyAccessToGroup()
        {

        }

        public void AddUserToGroup(int userId, int groupId)
        {
            var user = _context.Users.Find(userId);
            var group = _context.Groups.Find(groupId);
            if (user == null || group == null)
            {
                throw new Exception("User or group not found");
            }
            user.UserGroup.Add(new UserGroup { UserId = userId, GroupId = groupId });
        }

        public void RemoveUserFromGroup(int userId, int groupId)
        {

        }

        public Group CreateGroup(int UserId)
        {
            var group = new Group { Name = SampleUtils.GenerateUUID() };
            _context.Groups.Add(group);
            _context.SaveChanges();
            return group;
        }
    }
}
