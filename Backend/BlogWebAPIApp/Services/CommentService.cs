using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{
    public class CommentService : ICommentService
    {
        private readonly IRepository<Guid, Post> _posts;
        private readonly IRepository<Guid, Comment> _comments;
        private readonly IRepository<Guid, User> _users;

        public CommentService(
    IRepository<Guid, Post> posts,
    IRepository<Guid, Comment> comments,
    IRepository<Guid, User> users)
        {
            _posts = posts;
            _comments = comments;
            _users = users;
        }

        public async Task<Comment> Add(Guid postId, Guid authorId, string content, Guid? parentCommentId)
        {
            var author = await _users.Get(authorId)
    ?? throw new InvalidOperationException("User not found");

            // ✅ Block commenting if banned
            if (!author.CanComment)
                throw new InvalidOperationException("You are banned from commenting.");

            var post = await _posts.Get(postId) ?? throw new EntityNotFoundException("Post");
            if (!post.CommentsEnabled) throw new InvalidOperationException("Comments disabled for this post");

            Comment? parent = null;
            if (parentCommentId.HasValue)
            {
                parent = await _comments.Get(parentCommentId.Value);
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

            await _comments.Add(comment); // persists

            // Load Author for parity with original
            var loaded = await _comments.GetQueryable()
                                        .Include(x => x.Author)
                                        .FirstOrDefaultAsync(c => c.Id == comment.Id);

            return loaded ?? comment;
        }

        public async Task<Comment?> GetById(Guid commentId) =>
            await _comments.GetQueryable()
                           .Include(c => c.Author)
                           .FirstOrDefaultAsync(c => c.Id == commentId);

        public async Task Update(Guid commentId, Guid actorUserId, string content, string? status)
        {
            var comment = await _comments.GetQueryable()
                                         .Include(c => c.Post)
                                         .FirstOrDefaultAsync(c => c.Id == commentId)
                          ?? throw new EntityNotFoundException("Comment");

            // author can edit content; post author can moderate status
            var isAuthor = comment.AuthorId == actorUserId;
            var isPostAuthor = comment.Post.AuthorId == actorUserId;

            if (!isAuthor && !isPostAuthor) throw new UnAuthorizedException("Not allowed");

            comment.UpdatedAt = DateTime.UtcNow;
            if (isAuthor) comment.Content = content;
            if (isPostAuthor && status != null) comment.Status = System.Enum.Parse<CommentStatus>(status, true);

            await _comments.SaveChangesAsync();
        }

        public async Task Delete(Guid commentId, Guid actorUserId)
        {
            var comment = await _comments.GetQueryable()
                                         .Include(c => c.Post)
                                         .FirstOrDefaultAsync(c => c.Id == commentId)
                          ?? throw new EntityNotFoundException("Comment");

            var isAuthor = comment.AuthorId == actorUserId;
            var isPostAuthor = comment.Post.AuthorId == actorUserId;
            if (!isAuthor && !isPostAuthor) throw new UnAuthorizedException("Not allowed");

            await _comments.Delete(commentId); // persists
        }

        public async Task<(IReadOnlyList<Comment> items, int total)> GetByPost(Guid postId, int page, int pageSize)
        {
            var query = _comments.GetQueryable()
                                 .Include(c => c.Author)
                                 .Where(c => c.PostId == postId &&
                                             c.Status == CommentStatus.Approved &&
                                             c.ParentCommentId == null)
                                 .OrderBy(c => c.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            // Optionally load replies (same as original; attach in controller to avoid circular refs)
            var parentIds = items.Select(i => i.Id).ToList();
            var replies = await _comments.GetQueryable()
                                         .Include(c => c.Author)
                                         .Where(c => c.PostId == postId &&
                                                     c.ParentCommentId != null &&
                                                     parentIds.Contains(c.ParentCommentId!.Value))
                                         .OrderBy(c => c.CreatedAt)
                                         .ToListAsync();

            return (items, total);
        }

        public async Task<(IReadOnlyList<Comment> items, int total)> GetPendingComments(int page, int pageSize)
        {
            var query = _comments.GetQueryable()
                .Include(c => c.Author)
                .Include(c => c.Post)
                .Where(c => c.Status == CommentStatus.Pending && c.IsDeleted == false);

            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return (items, total);
        }

        public async Task AdminApprove(Guid commentId)
        {
            var comment = await _comments.Get(commentId)
                ?? throw new EntityNotFoundException("Comment");

            comment.Status = CommentStatus.Approved;
            await _comments.SaveChangesAsync();
        }

        public async Task AdminDelete(Guid commentId)
        {
            var comment = await _comments.Get(commentId)
                ?? throw new EntityNotFoundException("Comment");

            comment.IsDeleted = true;
            comment.Status = CommentStatus.Removed;

            await _comments.SaveChangesAsync();
        }
    }
}