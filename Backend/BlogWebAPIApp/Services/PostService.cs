using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{
    public class PostService : IPostService
    {
        private readonly IRepository<Guid, User> _users;
        private readonly IRepository<Guid, Post> _posts;
        private readonly IRepository<Guid, Tag> _tags;
        private readonly IRepository<Guid, Category> _categories;

        public PostService(
            IRepository<Guid, User> users,
            IRepository<Guid, Post> posts,
            IRepository<Guid, Tag> tags,
            IRepository<Guid, Category> categories)
        {
            _users = users;
            _posts = posts;
            _tags = tags;
            _categories = categories;
        }

        // ------------------ CREATE -------------------------
        public async Task<Post> Create(
            Guid authorId,
            string title,
            string? slug,
            string? excerpt,
            string contentHtml,
            string? contentMarkdown,
            string visibility,
            IEnumerable<string>? tagNames,
            IEnumerable<string>? categoryNames,
            bool? commentsEnabled,
            bool? autoApproveComments,
            string coverImageUrl)
        {
            var author = await _users.Get(authorId)
                ?? throw new InvalidOperationException("Author not found");

            var finalSlug = !string.IsNullOrWhiteSpace(slug)
                ? Slugify(slug)
                : Slugify(title);

            var exists = await _posts.GetQueryable().AnyAsync(p => p.Slug == finalSlug);

            if (exists)
                finalSlug = $"{finalSlug}-{Guid.NewGuid().ToString("N")[..6]}";

            var post = new Post
            {
                AuthorId = authorId,
                Title = title,
                Slug = finalSlug,
                Excerpt = excerpt,
                ContentHtml = contentHtml,
                ContentMarkdown = contentMarkdown,
                CoverImageUrl = coverImageUrl,
                Visibility =  System.Enum.Parse<Visibility>(visibility, true),
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
                var existing = await _tags.GetQueryable().Where(t => slugs.Contains(t.Slug)).ToListAsync();
                var missing = slugs.Except(existing.Select(e => e.Slug)).ToList();

                foreach (var m in missing)
                    await _tags.Add(new Tag { Name = m.Replace("-", " "), Slug = m });

                var tags = await _tags.GetQueryable().Where(t => slugs.Contains(t.Slug)).ToListAsync();

                foreach (var t in tags)
                    post.PostTags.Add(new PostTag { Post = post, Tag = t });
            }

            // Categories
            if (categoryNames != null)
            {
                var slugs = categoryNames.Select(Slugify).ToList();
                var existing = await _categories.GetQueryable().Where(c => slugs.Contains(c.Slug)).ToListAsync();
                var missing = slugs.Except(existing.Select(e => e.Slug)).ToList();

                foreach (var m in missing)
                    await _categories.Add(new Category { Name = m.Replace("-", " "), Slug = m });

                var cats = await _categories.GetQueryable().Where(c => slugs.Contains(c.Slug)).ToListAsync();

                foreach (var c in cats)
                    post.PostCategories.Add(new PostCategory { Post = post, Category = c });
            }

            await _posts.Add(post);

            return await _posts.GetQueryable()
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == post.Id)
                ?? post;
        }

        // ------------------ GET BY ID -------------------------
        public async Task<Post?> GetById(Guid id) =>
            await _posts.GetQueryable()
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

        // ------------------ GET BY SLUG -------------------------
        public async Task<Post?> GetBySlug(string slug) =>
            await _posts.GetQueryable()
                .Include(p => p.Author)
                .Include(p => p.Author.Followers)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug);

        // ------------------ UPDATE -------------------------
        public async Task<Post> Update(
            Guid postId,
            Guid actorUserId,
            string? title,
            string? slug,
            string? excerpt,
            string? contentHtml,
            string? contentMarkdown,
            string? visibility,
            IEnumerable<string>? tagNames,
            IEnumerable<string>? categoryNames,
            bool? commentsEnabled,
            bool? autoApproveComments,
            string? status,
            string coverImageUrl)
        {
            var post = await _posts.GetQueryable()
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == postId)
                ?? throw new EntityNotFoundException("Post");

            if (post.AuthorId != actorUserId)
                throw new UnAuthorizedException("Not author");

            if (title != null) post.Title = title;
            if (slug != null) post.Slug = Slugify(slug);
            if (excerpt != null) post.Excerpt = excerpt;
            if (contentHtml != null) post.ContentHtml = contentHtml;
            if (contentMarkdown != null) post.ContentMarkdown = contentMarkdown;

            if (commentsEnabled.HasValue)
                post.CommentsEnabled = commentsEnabled.Value;

            if (autoApproveComments.HasValue)
                post.AutoApproveComments = autoApproveComments.Value;

            if (visibility != null)
                post.Visibility = System.Enum.Parse<Visibility>(visibility, true);

            if (status != null)
                post.Status = System.Enum.Parse<PostStatus>(status, true);

            if (coverImageUrl != null)
                post.CoverImageUrl = coverImageUrl;

            // Tags
            if (tagNames != null)
            {
                post.PostTags.Clear();
                var slugs = tagNames.Select(Slugify).ToList();

                var existing = await _tags.GetQueryable().Where(t => slugs.Contains(t.Slug)).ToListAsync();
                var missing = slugs.Except(existing.Select(t => t.Slug)).ToList();

                foreach (var m in missing)
                    await _tags.Add(new Tag { Name = m.Replace("-", " "), Slug = m });

                var tags = await _tags.GetQueryable().Where(t => slugs.Contains(t.Slug)).ToListAsync();

                foreach (var t in tags)
                    post.PostTags.Add(new PostTag { PostId = post.Id, TagId = t.Id });
            }

            // Categories
            if (categoryNames != null)
            {
                post.PostCategories.Clear();

                var slugs = categoryNames.Select(Slugify).ToList();

                var existing = await _categories.GetQueryable().Where(c => slugs.Contains(c.Slug)).ToListAsync();
                var missing = slugs.Except(existing.Select(c => c.Slug)).ToList();

                foreach (var m in missing)
                    await _categories.Add(new Category { Name = m.Replace("-", " "), Slug = m });

                var cats = await _categories.GetQueryable().Where(c => slugs.Contains(c.Slug)).ToListAsync();

                foreach (var c in cats)
                    post.PostCategories.Add(new PostCategory { PostId = post.Id, CategoryId = c.Id });
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _posts.SaveChangesAsync();

            return post;
        }

        // ------------------ PUBLISH -------------------------
        public async Task Publish(Guid postId, Guid actorUserId)
        {
            var post = await _posts.Get(postId)
                ?? throw new EntityNotFoundException("Post");

            if (post.AuthorId != actorUserId)
                throw new UnAuthorizedException("Not author");

            // Submit for admin review — admin must approve before it goes live
            post.Status = PostStatus.Draft;
            post.IsRejected = false;
            post.UpdatedAt = DateTime.UtcNow;

            await _posts.SaveChangesAsync();
        }

        // ------------------ DELETE -------------------------
        public async Task Delete(Guid postId, Guid actorUserId)
        {
            var post = await _posts.Get(postId)
                ?? throw new EntityNotFoundException("Post");

            if (post.AuthorId != actorUserId)
                throw new UnAuthorizedException("Not author");

            await _posts.Delete(postId);
        }

        // ------------------ GET PUBLISHED POSTS (Public + follower-private) -------------------------
        public async Task<(IReadOnlyList<Post> items, int total)> GetPublished(
            int page,
            int pageSize,
            string? q,
            string? tagSlug,
            string? categorySlug,
            Guid? currentUserId)
        {
            var query = _posts.GetQueryable()
                .Include(p => p.Author)
                .Include(p => p.Author.Followers)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Where(p => p.Status == PostStatus.Published);

            query = query.Where(p =>
                p.Visibility == Visibility.Public ||
                (
                    p.Visibility == Visibility.Private &&
                    currentUserId.HasValue &&
                    (
                        p.AuthorId == currentUserId.Value ||
                        p.Author.Followers.Any(f => f.FollowerId == currentUserId.Value)
                    )
                )
            );

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p =>
                    p.Title.Contains(q) ||
                    p.ContentHtml.Contains(q) ||
                    (p.ContentMarkdown ?? "").Contains(q));

            if (!string.IsNullOrWhiteSpace(tagSlug))
                query = query.Where(p =>
                    p.PostTags.Any(pt => pt.Tag.Slug == tagSlug));

            if (!string.IsNullOrWhiteSpace(categorySlug))
                query = query.Where(p =>
                    p.PostCategories.Any(pc => pc.Category.Slug == categorySlug));

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }


        // ------------------ GET AUTHOR POSTS -------------------------
        public async Task<(IReadOnlyList<Post> items, int total)> GetByAuthor(Guid authorId, int page, int pageSize)
        {
            var query = _posts.GetQueryable()
                .Where(p => p.AuthorId == authorId)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Author)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .ToListAsync();

            return (items, total);
        }

        private static string Slugify(string input)
        {
            var chars = input.ToLower()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : (ch == ' ' ? '-' : '\0'))
                .Where(ch => ch != '\0')
                .ToArray();

            var s = new string(chars);
            while (s.Contains("--"))
                s = s.Replace("--", "-");

            return s.Trim('-');
        }

        public async Task<(IReadOnlyList<Post> items, int total)> GetPendingPosts(int page, int pageSize)
        {
            var query = _posts.GetQueryable()
                .Where(p => p.Status == PostStatus.Draft && p.IsRejected == false)
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .Include(p => p.Author)
                                   .ToListAsync();

            return (items, total);
        }

        public async Task ApprovePost(Guid postId)
        {
            var post = await _posts.Get(postId)
                ?? throw new InvalidOperationException("Post not found");

            post.Status = PostStatus.Published;
            post.IsRejected = false;
            post.PublishedAt = DateTime.UtcNow;

            await _posts.SaveChangesAsync();
        }

        public async Task RejectPost(Guid postId)
        {
            var post = await _posts.Get(postId)
                ?? throw new InvalidOperationException("Post not found");

            post.IsRejected = true;
            post.Status = PostStatus.Draft;

            await _posts.SaveChangesAsync();
        }

    

        public async Task AdminDelete(Guid postId)
        {
            var post = await _posts.Get(postId)
                ?? throw new InvalidOperationException("Post not found");

            await _posts.Delete(postId);
        }

        public async Task<(int total, int published, int draft, int pending)> GetPostStats()
        {
            var q = _posts.GetQueryable();
            var total     = await q.CountAsync();
            var published = await q.CountAsync(p => p.Status == PostStatus.Published);
            var draft     = await q.CountAsync(p => p.Status == PostStatus.Draft && p.IsRejected == false);
            var pending   = await q.CountAsync(p => p.Status == PostStatus.Draft && p.IsRejected == false);
            return (total, published, draft, pending);
        }
    }
}