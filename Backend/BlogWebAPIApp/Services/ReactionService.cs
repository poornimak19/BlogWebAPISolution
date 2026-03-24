using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlogWebAPIApp.Services
{
    public class ReactionService : IReactionService
    {
        private readonly IRepository<Guid, PostLike> _postLikes;
        private readonly IRepository<Guid, CommentLike> _commentLikes;

        public ReactionService(IRepository<Guid, PostLike> postLikes,
                               IRepository<Guid, CommentLike> commentLikes)
        {
            _postLikes = postLikes;
            _commentLikes = commentLikes;
        }

        public async Task<(bool liked, int totalLikes)> TogglePostLike(Guid postId, Guid userId)
        {
            var existing = await _postLikes.GetQueryable()
                                           .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
            if (existing != null)
            {
                await _postLikes.Delete(existing); // persists
                var count = await _postLikes.GetQueryable().CountAsync(l => l.PostId == postId);
                return (false, count);
            }

            await _postLikes.Add(new PostLike { PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow });
            var newCount = await _postLikes.GetQueryable().CountAsync(l => l.PostId == postId);
            return (true, newCount);
        }

        public async Task<(bool liked, int totalLikes)> ToggleCommentLike(Guid commentId, Guid userId)
        {
            var existing = await _commentLikes.GetQueryable()
                                              .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
            if (existing != null)
            {
                await _commentLikes.Delete(existing); // persists
                var count = await _commentLikes.GetQueryable().CountAsync(l => l.CommentId == commentId);
                return (false, count);
            }

            await _commentLikes.Add(new CommentLike { CommentId = commentId, UserId = userId, CreatedAt = DateTime.UtcNow });
            var newCount = await _commentLikes.GetQueryable().CountAsync(l => l.CommentId == commentId);
            return (true, newCount);
        }
    }
}