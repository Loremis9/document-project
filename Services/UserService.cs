using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WEBAPI_m1IL_1.Services
{
    public class UserService    
    {
        private readonly DocumentationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private RigthAccessService rigthAccessService;
        public UserService(DocumentationDbContext context,IHttpContextAccessor httpContextAccessor,RigthAccessService rigthAccessService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            this.rigthAccessService = rigthAccessService;
        }

        // CREATE
        public async Task<UserModel> CreateUserAsync(UserModel user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // READ ALL
        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        // READ BY ID
        public async Task<UserModel?> GetUserByIdAsync(int id)
        {
            var user =  await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if(user == null){
                throw new Exception("User or group not found");
            }
            return user;
        }

        // READ BY NAME
        public async Task<UserModel?> GetUserByNameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
        public async Task<UserModel?> GetUserByMailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
        }
        // UPDATE
        public async Task<bool> UpdateUser(UserModel user,int userId)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            
            if (existingUser == null) return false;
            existingUser.Username = user.Username;
            existingUser.EmailAddress = user.EmailAddress;
            existingUser.Password = user.Password;
            await _context.SaveChangesAsync();
            return true;
        }

        // DELETE
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        public UserModel? GetCurrentUser()
        {
            var identity = _httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                var userClaims = identity.Claims;
                var idValue = userClaims.FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(idValue, out int id))
                {
                    return new UserModel
                    {
                        Id = id,
                    };
                }
            }
            return null;
        }

    }
}