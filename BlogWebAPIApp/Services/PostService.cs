using BlogWebAPIApp.Context;
using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{

    public class PostService : IPostService
    {
        private readonly BlogContext _db;

        public PostService(BlogContext db) => _db = db;

        public async Task<Post> Create(Guid authorId,
                                       string title,
                                       string? slug,
                                       string? excerpt,
                                       string contentHtml,
                                       string? contentMarkdown,
                                       string visibility,
                                       IEnumerable<string>? tagNames,
                                       IEnumerable<string>? categoryNames,
                                       IEnumerable<Guid>? allowedUserIds,
                                       bool? commentsEnabled,
                                       bool? autoApproveComments)
         {
            var author = await _db.Users.FindAsync(authorId) ?? throw new InvalidOperationException("Author not found");

            var finalSlug = !string.IsNullOrWhiteSpace(slug) ? Slugify(slug) : Slugify(title);
            if (await _db.Posts.AnyAsync(p => p.Slug == finalSlug))
                finalSlug = $"{finalSlug}-{Guid.NewGuid().ToString("N")[..6]}";

            var post = new Post
            {
                AuthorId = authorId,
                Title = title,
                Slug = finalSlug,
                Excerpt = excerpt,
                ContentHtml = contentHtml,
                ContentMarkdown = contentMarkdown,
                CoverImageUrl = "",
                Visibility = System.Enum.Parse<Visibility>(visibility, true),
                CommentsEnabled = commentsEnabled ?? true,
                AutoApproveComments = autoApproveComments ?? true,
                Status = PostStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Tags
            if (tagNames != null)
            {
                var slugs = tagNames.Select(Slugify).ToList();
                var existing = await _db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync();
                var missing = slugs.Except(existing.Select(e => e.Slug)).ToList();
                foreach (var m in missing)
                    _db.Tags.Add(new Tag { Name = m.Replace("-", " "), Slug = m });
                if (missing.Count > 0) await _db.SaveChangesAsync();

                var tags = await _db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync();
                foreach (var t in tags) post.PostTags.Add(new PostTag { Post = post, Tag = t });
            }

            // Categories
            if (categoryNames != null)
            {
                var slugs = categoryNames.Select(Slugify).ToList();
                var existing = await _db.Categories.Where(c => slugs.Contains(c.Slug)).ToListAsync();
                var missing = slugs.Except(existing.Select(e => e.Slug)).ToList();
                foreach (var m in missing)
                    _db.Categories.Add(new Category { Name = m.Replace("-", " "), Slug = m });
                if (missing.Count > 0) await _db.SaveChangesAsync();

                var cats = await _db.Categories.Where(c => slugs.Contains(c.Slug)).ToListAsync();
                foreach (var c in cats) post.PostCategories.Add(new PostCategory { Post = post, Category = c });
            }

            // Audience for Restricted
            if (post.Visibility == Visibility.Restricted && allowedUserIds != null)
            {
                foreach (var uid in allowedUserIds.Distinct())
                    post.Audience.Add(new PostAudience { Post = post, UserId = uid });
            }

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            post = await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Audience)
                .FirstOrDefaultAsync(p => p.Id == post.Id)
                ?? post;

            return post;
        }


        public async Task<Post?> GetById(Guid postId) =>
            await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Audience)
                .FirstOrDefaultAsync(p => p.Id == postId);

        public async Task<Post?> GetBySlug(string slug) =>
            await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Audience)
                .FirstOrDefaultAsync(p => p.Slug == slug);


        public async Task<Post> Update(Guid postId,
                                       Guid actorUserId,
                                       string? title,
                                       string? slug,
                                       string? excerpt,
                                       string? contentHtml,
                                       string? contentMarkdown,
                                       string? visibility,
                                       IEnumerable<string>? tagNames,
                                       IEnumerable<string>? categoryNames,
                                       IEnumerable<Guid>? allowedUserIds,
                                       bool? commentsEnabled,
                                       bool? autoApproveComments,
                                       string? status)
        {
            var post = await _db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Audience)
                .FirstOrDefaultAsync(p => p.Id == postId) ?? throw new EntityNotFoundException("Post");

            if (post.AuthorId != actorUserId)
                throw new UnAuthorizedException("Not the author");

            if (title != null) post.Title = title;
            if (slug != null) post.Slug = Slugify(slug);
            if (excerpt != null) post.Excerpt = excerpt;
            if (contentHtml != null) post.ContentHtml = contentHtml;
            if (contentMarkdown != null) post.ContentMarkdown = contentMarkdown;
            if (commentsEnabled.HasValue) post.CommentsEnabled = commentsEnabled.Value;
            if (autoApproveComments.HasValue) post.AutoApproveComments = autoApproveComments.Value;
            if (visibility != null) post.Visibility = System.Enum.Parse<Visibility>(visibility, true);
            if (status != null) post.Status = System.Enum.Parse<PostStatus>(status, true);

            // Replace tags/categories if provided
            if (tagNames != null)
            {
                post.PostTags.Clear();
                var slugs = tagNames.Select(Slugify).ToList();
                var existing = await _db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync();
                var miss = slugs.Except(existing.Select(e => e.Slug)).ToList();
                foreach (var m in miss) _db.Tags.Add(new Tag { Name = m.Replace("-", " "), Slug = m });
                if (miss.Count > 0) await _db.SaveChangesAsync();
                var tags = await _db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync();
                foreach (var t in tags) post.PostTags.Add(new PostTag { PostId = post.Id, TagId = t.Id });
            }

            if (categoryNames != null)
            {
                post.PostCategories.Clear();
                var slugs = categoryNames.Select(Slugify).ToList();
                var existing = await _db.Categories.Where(c => slugs.Contains(c.Slug)).ToListAsync();
                var miss = slugs.Except(existing.Select(e => e.Slug)).ToList();
                foreach (var m in miss) _db.Categories.Add(new Category { Name = m.Replace("-", " "), Slug = m });
                if (miss.Count > 0) await _db.SaveChangesAsync();
                var cats = await _db.Categories.Where(c => slugs.Contains(c.Slug)).ToListAsync();
                foreach (var c in cats) post.PostCategories.Add(new PostCategory { PostId = post.Id, CategoryId = c.Id });
            }

            if (post.Visibility == Visibility.Restricted && allowedUserIds != null)
            {
                post.Audience.Clear();
                foreach (var uid in allowedUserIds.Distinct())
                    post.Audience.Add(new PostAudience { PostId = post.Id, UserId = uid });
            }
            else if (post.Visibility != Visibility.Restricted)
            {
                post.Audience.Clear();
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return post;
        }

        public async Task Publish(Guid postId, Guid actorUserId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId)
                ?? throw new EntityNotFoundException("Post");
            if (post.AuthorId != actorUserId)
                throw new UnAuthorizedException("Not the author");
            post.Status = PostStatus.Published;
            post.PublishedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task Delete(Guid postId, Guid actorUserId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId)
                ?? throw new EntityNotFoundException("Post");
            if (post.AuthorId != actorUserId)
                throw new UnAuthorizedException("Not the author");
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
        }

        public async Task<(IReadOnlyList<Post> items, int total)> GetPublished(int page, int pageSize, string? q, string? tagSlug, string? categorySlug)
        {
            var query = _db.Posts
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Where(p => p.Status == PostStatus.Published && p.Visibility == Visibility.Public);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Title.Contains(q) || (p.ContentMarkdown ?? "").Contains(q) || p.ContentHtml.Contains(q));

            if (!string.IsNullOrWhiteSpace(tagSlug))
                query = query.Where(p => p.PostTags.Any(pt => pt.Tag.Slug == tagSlug));

            if (!string.IsNullOrWhiteSpace(categorySlug))
                query = query.Where(p => p.PostCategories.Any(pc => pc.Category.Slug == categorySlug));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(IReadOnlyList<Post> items, int total)> GetByAuthor(Guid authorId, int page, int pageSize)
        {
            var query = _db.Posts
                .Where(p => p.AuthorId == authorId)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .ToListAsync();
            return (items, total);
        }

        private static string Slugify(string input)
        {
            var chars = input.ToLower().Select(ch => char.IsLetterOrDigit(ch) ? ch : (ch == ' ' ? '-' : '\0'))
                .Where(ch => ch != '\0').ToArray();
            var s = new string(chars);
            while (s.Contains("--")) s = s.Replace("--", "-");
            return s.Trim('-');
        }
    }

}
