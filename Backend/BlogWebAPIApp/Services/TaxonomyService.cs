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

        private readonly IRepository<int, Tag> _tags;
        private readonly IRepository<int, Category> _categories;

        public TaxonomyService(
            IRepository<int, Tag> tags,
            IRepository<int, Category> categories)
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


        public async Task<Tag> RenameTag(int id, string newName)
        {
            var newSlug = Slugify(newName);

            var conflict = await _tags.GetQueryable()
                .AnyAsync(t =>
                    t.Id != id &&
                    (t.Name == newName || t.Slug == newSlug));

            if (conflict)
                throw new InvalidOperationException(
                    $"A tag with name or slug '{newName}' already exists.");

            var tag = await _tags.Get(id)
                ?? throw new InvalidOperationException("Tag not found");

            tag.Name = newName;
            tag.Slug = newSlug;

            await _tags.SaveChangesAsync();
            return tag;
        }


        public async Task DeleteTag(int id)
        {
            await _tags.Delete(id);
        }

        public async Task<Category> RenameCategory(int id, string newName)
        {
            var newSlug = Slugify(newName);

            // 1. Check for name OR slug conflict (excluding current category)
            var conflict = await _categories.GetQueryable()
                .AnyAsync(c =>
                    c.Id != id &&
                    (c.Name == newName || c.Slug == newSlug));

            if (conflict)
                throw new InvalidOperationException(
                    $"A category with name or slug '{newName}' already exists.");

            // 2. Fetch category
            var category = await _categories.Get(id)
                ?? throw new InvalidOperationException("Category not found");

            // 3. Apply rename
            category.Name = newName;
            category.Slug = newSlug;

            await _categories.SaveChangesAsync();
            return category;
        }

        public async Task DeleteCategory(int id)
        {
            await _categories.Delete(id);
        }
    }
}