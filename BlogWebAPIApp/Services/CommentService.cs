using BlogWebAPIApp.Context;
using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{

    public class CommentService : ICommentService
    {
        private readonly BlogContext _db;

        public CommentService(BlogContext db) => _db = db;

        public async Task<Comment> Add(Guid postId, Guid authorId, string content, Guid? parentCommentId)
        {
            var post = await _db.Posts.FindAsync(postId) ?? throw new EntityNotFoundException("Post");
            if (!post.CommentsEnabled) throw new InvalidOperationException("Comments disabled for this post");

            Comment? parent = null;
            if (parentCommentId.HasValue)
            {
                parent = await _db.Comments.FindAsync(parentCommentId.Value);
                if (parent == null || parent.PostId != postId)
                    throw new InvalidOperationException("Invalid parent comment");
            }

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = authorId,
                ParentCommentId = parent?.Id,
                Content = content,
                Status = post.AutoApproveComments ? CommentStatus.Approved : CommentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
            await _db.Entry(comment).Reference(x => x.Author).LoadAsync();
            return comment;
        }

        public async Task<Comment?> GetById(Guid commentId) =>
            await _db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == commentId);

        public async Task Update(Guid commentId, Guid actorUserId, string content, string? status)
        {
            var comment = await _db.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == commentId)
                ?? throw new EntityNotFoundException("Comment");

            // author can edit content; post author can moderate status
            var isAuthor = comment.AuthorId == actorUserId;
            var isPostAuthor = comment.Post.AuthorId == actorUserId;

            if (!isAuthor && !isPostAuthor) throw new UnAuthorizedException("Not allowed");

            comment.UpdatedAt = DateTime.UtcNow;
            if (isAuthor) comment.Content = content;
            if (isPostAuthor && status != null) comment.Status = System.Enum.Parse<CommentStatus>(status, true);

            await _db.SaveChangesAsync();
        }

        public async Task Delete(Guid commentId, Guid actorUserId)
        {
            var comment = await _db.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == commentId)
                ?? throw new EntityNotFoundException("Comment");

            var isAuthor = comment.AuthorId == actorUserId;
            var isPostAuthor = comment.Post.AuthorId == actorUserId;
            if (!isAuthor && !isPostAuthor) throw new UnAuthorizedException("Not allowed");

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
        }

        public async Task<(IReadOnlyList<Comment> items, int total)> GetByPost(Guid postId, int page, int pageSize)
        {
            var query = _db.Comments
                .Include(c => c.Author)
                .Where(c => c.PostId == postId && c.Status == CommentStatus.Approved && c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Optionally load replies
            var parentIds = items.Select(i => i.Id).ToList();
            var replies = await _db.Comments
                .Include(c => c.Author)
                .Where(c => c.PostId == postId && c.ParentCommentId != null && parentIds.Contains(c.ParentCommentId.Value))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
            // You can attach replies to parent in controller if you want to avoid circular refs

            return (items, total);
        }
    }

}
