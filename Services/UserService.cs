using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WEBAPI_m1IL_1.DTO;
namespace WEBAPI_m1IL_1.Services
{
    public class UserService    
    {
        private readonly DocumentationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService(DocumentationDbContext context,IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // CREATE
        public async Task<UserModel> CreateUserAsync(string username, string emailAddress,string password)
        {
            var user = new UserModel {
                Username = username,
                EmailAddress =  emailAddress,
                Password = password
            };

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


        public async Task<UserModel?> GetUserByNameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<UserModel?> GetUserByMailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
        }
        // UPDATE
        public async Task<OutputUserDto> UpdateUser(string? username,string? password,string? email,int userId)
        {
            var existingUser = await _context.Users.FindAsync(userId);
            
            if (existingUser == null) return null;
            if(string.IsNullOrEmpty(username))
                existingUser.Username = username;
            if(string.IsNullOrEmpty(email))
                existingUser.EmailAddress = email;
            if(string.IsNullOrEmpty(password))
                existingUser.Password = password;
            await _context.SaveChangesAsync();
            var updatedUser = await _context.Users.FindAsync(userId);
            return new OutputUserDto { Id = updatedUser.Id,
                EmailAddress = updatedUser.EmailAddress,
                Username = updatedUser.Username
            };
        }

        // DELETE
        public async Task<bool> DeleteUser(int id, int UserToDelete)
        {
            var userToDeleteIsAdmin = await _context.Users.FindAsync(id);
            if(!userToDeleteIsAdmin.IsGlobalAdmin)
                throw new Exception("UserToDelete is not globalAdmin");

            var user = await _context.Users.FindAsync(UserToDelete);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<UserModel?> GetCurrentUserAsync()
        {
            var identity = _httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                var userClaims = identity.Claims;
                var idValue = userClaims.FirstOrDefault(o => o.Type == ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(idValue, out int id))
                {
                    return await GetUserByIdAsync(id);
                }
            }
            return null;
        }

    }
}