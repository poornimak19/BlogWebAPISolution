using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace BlogWebAPIApp.Services
{

    public class UserService : IUserService
    {
        private readonly BlogContext _db;

        public UserService(BlogContext db) => _db = db;

        public async Task<User?> GetByUsername(string username) =>
            await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

        public async Task<User?> GetById(Guid id) => await _db.Users.FindAsync(id);

        public async Task UpdateProfile(Guid userId, string? displayName, string? bio, string? avatarUrl)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) throw new InvalidOperationException("User not found");

            if (displayName != null) user.DisplayName = displayName;
            if (bio != null) user.Bio = bio;
            if (avatarUrl != null) user.AvatarUrl = avatarUrl;

            await _db.SaveChangesAsync();
        }

        public async Task<(int followers, int following)> GetFollowCounts(Guid userId)
        {
            var followers = await _db.Follows.CountAsync(f => f.FolloweeId == userId);
            var following = await _db.Follows.CountAsync(f => f.FollowerId == userId);
            return (followers, following);
        }
    }

}
