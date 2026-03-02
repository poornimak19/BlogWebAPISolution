using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Services
{

    public class SearchService : ISearchService
    {
        private readonly BlogContext _db;

        public SearchService(BlogContext db) => _db = db;

        public async Task<(IReadOnlyList<Post> items, int total)> SearchPosts(int page, int pageSize,
                                                                             string? q,
                                                                             string? tagSlug,
                                                                             string? categorySlug,
                                                                             string? authorUsername,
                                                                             string? sort)
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

            if (!string.IsNullOrWhiteSpace(authorUsername))
                query = query.Where(p => p.Author.Username == authorUsername);

            query = (sort?.ToLower()) switch
            {
                "popular" => query.OrderByDescending(p => p.Likes.Count).ThenByDescending(p => p.PublishedAt ?? p.CreatedAt),
                _ => query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }
    }

}
