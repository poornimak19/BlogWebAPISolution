using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

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
    }
}