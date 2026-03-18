using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlogWebAPIApp.Services
{
    public class FollowService : IFollowService
    {
        private readonly IRepository<Guid, Follow> _follows;

        public FollowService(IRepository<Guid, Follow> follows)
        {
            _follows = follows;
        }

        public async Task<(bool following, int followersCount)> ToggleFollow(Guid followerId, Guid followeeId)
        {
            if (followerId == followeeId) throw new InvalidOperationException("Cannot follow yourself");

            var existing = await _follows.GetQueryable()
                                         .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
            if (existing != null)
            {
                // Composite key delete
                await _follows.Delete(existing); // persists
                var followersCount = await _follows.GetQueryable().CountAsync(f => f.FolloweeId == followeeId);
                return (false, followersCount);
            }

            await _follows.Add(new Follow { FollowerId = followerId, FolloweeId = followeeId, CreatedAt = DateTime.UtcNow });
            var countAfter = await _follows.GetQueryable().CountAsync(f => f.FolloweeId == followeeId);
            return (true, countAfter);
        }

        public async Task<(int followers, int following)> GetCounts(Guid userId)
        {
            var followers = await _follows.GetQueryable().CountAsync(f => f.FolloweeId == userId);
            var following = await _follows.GetQueryable().CountAsync(f => f.FollowerId == userId);
            return (followers, following);
        }
    }
}