using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace BlogWebAPIApp.Services
{

    public class ReactionService : IReactionService
    {
        private readonly BlogContext _db;

        public ReactionService(BlogContext db) => _db = db;

        public async Task<(bool liked, int totalLikes)> TogglePostLike(Guid postId, Guid userId)
        {
            var existing = await _db.PostLikes.FindAsync(userId, postId);
            if (existing != null)
            {
                _db.PostLikes.Remove(existing);
                await _db.SaveChangesAsync();
                var count = await _db.PostLikes.CountAsync(l => l.PostId == postId);
                return (false, count);
            }
            _db.PostLikes.Add(new PostLike { PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            return (true, await _db.PostLikes.CountAsync(l => l.PostId == postId));
        }

        public async Task<(bool liked, int totalLikes)> ToggleCommentLike(Guid commentId, Guid userId)
        {
            var existing = await _db.CommentLikes.FindAsync(userId, commentId);
            if (existing != null)
            {
                _db.CommentLikes.Remove(existing);
                await _db.SaveChangesAsync();
                var count = await _db.CommentLikes.CountAsync(l => l.CommentId == commentId);
                return (false, count);
            }
            _db.CommentLikes.Add(new CommentLike { CommentId = commentId, UserId = userId, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            return (true, await _db.CommentLikes.CountAsync(l => l.CommentId == commentId));
        }
    }

}
