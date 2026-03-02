using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace BlogWebAPIApp.Services
{

    public class TaxonomyService : ITaxonomyService
    {
        private readonly BlogContext _db;

        public TaxonomyService(BlogContext db) => _db = db;

        public async Task<IReadOnlyList<Tag>> GetAllTags() =>
            await _db.Tags.OrderBy(t => t.Name).ToListAsync();

        public async Task<IReadOnlyList<Category>> GetAllCategories() =>
            await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        public async Task<Tag> EnsureTag(string name)
        {
            var slug = Slugify(name);
            var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (existing != null) return existing;
            var tag = new Tag { Name = name, Slug = slug };
            _db.Tags.Add(tag);
            await _db.SaveChangesAsync();
            return tag;
        }

        public async Task<Category> EnsureCategory(string name)
        {
            var slug = Slugify(name);
            var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (existing != null) return existing;
            var cat = new Category { Name = name, Slug = slug };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return cat;
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
