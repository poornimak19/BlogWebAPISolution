using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace BlogWebAPIApp.Services
{

    public class FollowService : IFollowService
    {
        private readonly BlogContext _db;

        public FollowService(BlogContext db) => _db = db;

        public async Task<(bool following, int followersCount)> ToggleFollow(Guid followerId, Guid followeeId)
        {
            if (followerId == followeeId) throw new InvalidOperationException("Cannot follow yourself");

            var existing = await _db.Follows.FindAsync(followerId, followeeId);
            if (existing != null)
            {
                _db.Follows.Remove(existing);
                await _db.SaveChangesAsync();
                var followersCount = await _db.Follows.CountAsync(f => f.FolloweeId == followeeId);
                return (false, followersCount);
            }
            _db.Follows.Add(new Follow { FollowerId = followerId, FolloweeId = followeeId, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            return (true, await _db.Follows.CountAsync(f => f.FolloweeId == followeeId));
        }

        public async Task<(int followers, int following)> GetCounts(Guid userId)
        {
            var followers = await _db.Follows.CountAsync(f => f.FolloweeId == userId);
            var following = await _db.Follows.CountAsync(f => f.FollowerId == userId);
            return (followers, following);
        }
    }

}
