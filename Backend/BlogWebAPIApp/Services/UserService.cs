using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<Guid, User> _users;
        private readonly IRepository<Guid, Follow> _follows;

        public UserService(IRepository<Guid, User> users,
                           IRepository<Guid, Follow> follows)
        {
            _users = users;
            _follows = follows;
        }

        public async Task<User?> GetByUsername(string username) =>
            await _users.GetQueryable().FirstOrDefaultAsync(u => u.Username == username);

        public async Task<User?> GetById(Guid id) => await _users.Get(id);

        public async Task UpdateProfile(Guid userId, string? displayName, string? bio, string? avatarUrl)
        {
            var user = await _users.Get(userId);
            if (user == null) throw new InvalidOperationException("User not found");

            if (displayName != null) user.DisplayName = displayName;
            if (bio != null) user.Bio = bio;
            if (avatarUrl != null) user.AvatarUrl = avatarUrl;

            // Commit tracked changes (replaces _db.SaveChangesAsync())
            await _users.SaveChangesAsync();
        }

        public async Task<(int followers, int following)> GetFollowCounts(Guid userId)
        {
            var followers = await _follows.GetQueryable().CountAsync(f => f.FolloweeId == userId);
            var following = await _follows.GetQueryable().CountAsync(f => f.FollowerId == userId);
            return (followers, following);
        }

        public async Task<IEnumerable<User>> SearchUsers(string query)
        {
            return await _users.GetQueryable()
                .Where(u => u.Username.Contains(query) ||
                            (u.DisplayName != null && u.DisplayName.Contains(query)))
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _users.GetQueryable()
                .Include(u => u.Posts)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task ChangeRole(Guid userId, UserRole newRole)
        {
            var user = await _users.Get(userId)
                ?? throw new InvalidOperationException("User not found");

            user.Role = newRole;
            await _users.SaveChangesAsync();
        }

        public async Task SuspendUser(Guid userId, bool suspend)
        {
            var user = await _users.Get(userId)
                ?? throw new InvalidOperationException("User not found");

            user.IsSuspended = suspend;
            await _users.SaveChangesAsync();
        }

        public async Task DeleteUser(Guid userId)
        {
            var user = await _users.Get(userId)
                ?? throw new InvalidOperationException("User not found");

            await _users.Delete(userId);
        }

        public async Task SetCommentPermission(Guid userId, bool canComment)
        {
            var user = await _users.Get(userId)
                ?? throw new InvalidOperationException("User not found");

            user.CanComment = canComment;
            await _users.SaveChangesAsync();
        }

       
    }
}