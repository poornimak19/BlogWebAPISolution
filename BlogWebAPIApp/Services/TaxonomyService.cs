using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlogWebAPIApp.Services
{
    public class TaxonomyService : ITaxonomyService
    {
        private readonly IRepository<Guid, Tag> _tags;
        private readonly IRepository<Guid, Category> _categories;

        public TaxonomyService(IRepository<Guid, Tag> tags, IRepository<Guid, Category> categories)
        {
            _tags = tags;
            _categories = categories;
        }

        public async Task<IReadOnlyList<Tag>> GetAllTags() =>
            await _tags.GetQueryable().OrderBy(t => t.Name).ToListAsync();

        public async Task<IReadOnlyList<Category>> GetAllCategories() =>
            await _categories.GetQueryable().OrderBy(c => c.Name).ToListAsync();

        public async Task<Tag> EnsureTag(string name)
        {
            var slug = Slugify(name);
            var existing = await _tags.GetQueryable().FirstOrDefaultAsync(t => t.Slug == slug);
            if (existing != null) return existing;

            var tag = new Tag { Name = name, Slug = slug };
            await _tags.Add(tag); // persists
            return tag;
        }

        public async Task<Category> EnsureCategory(string name)
        {
            var slug = Slugify(name);
            var existing = await _categories.GetQueryable().FirstOrDefaultAsync(c => c.Slug == slug);
            if (existing != null) return existing;

            var cat = new Category { Name = name, Slug = slug };
            await _categories.Add(cat); // persists
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