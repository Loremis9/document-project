using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Models;
using WEBAPI_m1IL_1.Utils;
using System;


namespace WEBAPI_m1IL_1.Services
{
    public class RigthAccessService
    {
        private readonly DocumentationDbContext _context;
        private UserService userService;

        public RigthAccessService(DocumentationDbContext context, UserService userService)
        {
            _context = context;
            this.userService = userService;
        }

        public async Task AddFirstUserToDocumentation(int userId, int documentationId){
            var user = await userService.GetUserByIdAsync(userId);
            await CreateUserPermission(user, documentationId);
        }
        public async Task AddUserToDocumentation(int userId, int documentationId, bool? read, bool? write, bool? delete, bool? admin){
            var user = await userService.GetUserByIdAsync(userId);
            var userPermissions = await GetUserPermission(userId,documentationId);
            await ChangePermissionUser(userPermissions,read,write,delete,admin);
        }
        public async Task CreateUserPermission(UserModel user, int documentationId)
        {
           var permission = new UserPermission
           {
               UserId = user.Id,
               DocumentationId = documentationId,
               CanRead = true,
               CanWrite = true,
               CanDelete = true,
               IsAdmin = true

           };
            await _context.UserPermissions.AddAsync(permission);
            await _context.SaveChangesAsync();
        }

        public async Task<UserPermission> ChangePermissionUser(UserPermission userPermissions, bool? read, bool? write, bool? delete, bool? admin){
            if (read.HasValue) userPermissions.CanRead = read.Value;
            if (write.HasValue) userPermissions.CanWrite = write.Value;
            if (delete.HasValue) userPermissions.CanDelete = delete.Value;
            if (admin.HasValue) userPermissions.IsAdmin = admin.Value;
            await _context.SaveChangesAsync();
            return await GetUserPermission(userPermissions.UserId,userPermissions.DocumentationId);
        }
        public async Task<UserPermission> ChangeUserPermisionByOtherUser(int userId, int documentationId, bool? read, bool? write, bool? delete, bool? admin,int userIdToChange){
            var userPermissions = await GetUserPermission(userId,documentationId);
            if(!userPermissions.IsAdmin) return null;
            var userToChangePermissions = await GetUserPermission(userIdToChange,documentationId);
            var updatedPermission = await ChangePermissionUser(userPermissions,  read,  write,  delete, admin);
            return updatedPermission;
        }


        public async Task<bool> DeletePermission(int userId,int documentationId,int UserToDelete){
            var user = await userService.GetUserByIdAsync(userId);
            var userPermissions = await GetUserPermission(userId,documentationId);
            if(!userPermissions.IsAdmin) return false;
            var userPermissionsToDelete =  _context.UserPermissions.Where(u => u.UserId == UserToDelete && u.DocumentationId == documentationId);
                _context.UserPermissions.RemoveRange(userPermissionsToDelete);
             await _context.SaveChangesAsync();
             return true;
        }

        public async Task<UserPermission> GetUserPermission(int userId, int documentationId){
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(u => u.DocumentationId == documentationId && u.UserId == userId);
            
            if (userPermission == null)
            {
                throw new InvalidOperationException("Permissions not found for this user and documentation.");
            }
            return userPermission;
        }

        public async Task<ICollection<UserPermission>> GetAllUserPermission(int userId){
                var userPermission = await _context.UserPermissions
                .Where(u => u.UserId == userId)
                .ToListAsync();
            if (userPermission == null)
            {
                throw new InvalidOperationException("Permissions not found");
            }
            return userPermission;
        }
        

        public async Task<bool> HavePermissionTo(int userId,int documentationId,string right){
            var userPermission = await  _context.UserPermissions.FirstOrDefaultAsync(u => u.DocumentationId == documentationId && u.UserId == userId);
                if (userPermission == null)
                return false;
            switch(right){
                case "read":
                return userPermission.CanRead;
                case "write":
                return userPermission.CanWrite;
                case "delete":
                return userPermission.CanDelete;
                case "admin":
                return userPermission.IsAdmin;
                default:
                throw new InvalidOperationException("User don't Have permissions for this documentation.");
        }
    }
}
}