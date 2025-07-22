using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WEBAPI_m1IL_1.Models;

namespace WEBAPI_m1IL_1.Services
{
    public class UserService    
    {
        private readonly DocumentationDbContext _context;

        public UserService(DocumentationDbContext context)
        {
            _context = context;
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
            return await _context.Users.Include(u => u.UserGroup).ToListAsync();
        }

        // READ BY ID
        public async Task<UserModel?> GetUserByIdAsync(int id)
        {
            return await _context.Users.Include(u => u.UserGroup).FirstOrDefaultAsync(u => u.Id == id);
        }

        // READ BY NAME
        public async Task<UserModel?> GetUserByNameAsync(string username)
        {
            return await _context.Users.Include(u => u.UserGroup).FirstOrDefaultAsync(u => u.Username == username);
        }

        // UPDATE
        public async Task<bool> UpdateUserAsync(UserModel user)
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
    }
}