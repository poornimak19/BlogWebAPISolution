using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;

namespace BlogAppTest.Services
{
    public class TaxonomyServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<int, Tag>      _tags;
        private readonly InMemoryRepository<int, Category> _categories;
        private readonly TaxonomyService _sut;

        public TaxonomyServiceTests()
        {
            _db         = TestDbContextFactory.Create();
            _tags       = new InMemoryRepository<int, Tag>(_db);
            _categories = new InMemoryRepository<int, Category>(_db);
            _sut        = new TaxonomyService(_tags, _categories);
        }

        public void Dispose() => _db.Dispose();

        // ── GetAllTags ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllTags_ShouldReturnTagsOrderedByName()
        {
            _tags.Seed([new Tag { Name = "Zebra", Slug = "zebra" }, new Tag { Name = "Apple", Slug = "apple" }]);

            var result = await _sut.GetAllTags();

            Assert.Equal(2, result.Count);
            Assert.Equal("Apple", result[0].Name);
        }

        [Fact]
        public async Task GetAllTags_ShouldReturnEmpty_WhenNoTags()
        {
            var result = await _sut.GetAllTags();
            Assert.Empty(result);
        }

        // ── GetAllCategories ──────────────────────────────────────────────────

        [Fact]
        public async Task GetAllCategories_ShouldReturnCategoriesOrderedByName()
        {
            _categories.Seed([new Category { Name = "Tech", Slug = "tech" }, new Category { Name = "Art", Slug = "art" }]);

            var result = await _sut.GetAllCategories();

            Assert.Equal(2, result.Count);
            Assert.Equal("Art", result[0].Name);
        }

        // ── EnsureTag ─────────────────────────────────────────────────────────

        [Fact]
        public async Task EnsureTag_ShouldReturnExistingTag_WhenSlugMatches()
        {
            _tags.Seed([new Tag { Name = "dotnet", Slug = "dotnet" }]);

            var result = await _sut.EnsureTag("dotnet");

            Assert.Equal("dotnet", result.Slug);
            Assert.Equal(1, _db.Tags.Count());
        }

        [Fact]
        public async Task EnsureTag_ShouldCreateAndReturnNewTag_WhenNotExists()
        {
            var result = await _sut.EnsureTag("dotnet");

            Assert.Equal("dotnet", result.Name);
            Assert.Equal("dotnet", result.Slug);
            Assert.Equal(1, _db.Tags.Count());
        }

        // ── EnsureCategory ────────────────────────────────────────────────────

        [Fact]
        public async Task EnsureCategory_ShouldReturnExistingCategory_WhenSlugMatches()
        {
            _categories.Seed([new Category { Name = "Tech", Slug = "tech" }]);

            var result = await _sut.EnsureCategory("Tech");

            Assert.Equal("tech", result.Slug);
            Assert.Equal(1, _db.Categories.Count());
        }

        [Fact]
        public async Task EnsureCategory_ShouldCreateNewCategory_WhenNotExists()
        {
            var result = await _sut.EnsureCategory("Science");

            Assert.Equal("Science", result.Name);
            Assert.Equal("science", result.Slug);
        }

        // ── RenameTag ─────────────────────────────────────────────────────────

        [Fact]
        public async Task RenameTag_ShouldUpdateNameAndSlug_WhenNoConflict()
        {
            _tags.Seed([new Tag { Name = "Old", Slug = "old" }]);
            var tag = _db.Tags.First();

            var result = await _sut.RenameTag(tag.Id, "New Name");

            Assert.Equal("New Name", result.Name);
            Assert.Equal("new-name", result.Slug);
        }

        [Fact]
        public async Task RenameTag_ShouldThrow_WhenNameConflictsWithAnotherTag()
        {
            _tags.Seed([new Tag { Name = "Old", Slug = "old" }, new Tag { Name = "Conflict", Slug = "conflict" }]);
            var tag = _db.Tags.First(t => t.Name == "Old");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.RenameTag(tag.Id, "Conflict"));
        }

        [Fact]
        public async Task RenameTag_ShouldThrow_WhenTagNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.RenameTag(9999, "Anything"));
        }

        // ── DeleteTag ─────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteTag_ShouldRemoveTag_WhenExists()
        {
            _tags.Seed([new Tag { Name = "ToDelete", Slug = "to-delete" }]);
            var tag = _db.Tags.First();

            await _sut.DeleteTag(tag.Id);

            Assert.Equal(0, _db.Tags.Count());
        }

        // ── RenameCategory ────────────────────────────────────────────────────

        [Fact]
        public async Task RenameCategory_ShouldUpdateNameAndSlug_WhenNoConflict()
        {
            _categories.Seed([new Category { Name = "Old", Slug = "old" }]);
            var cat = _db.Categories.First();

            var result = await _sut.RenameCategory(cat.Id, "New Cat");

            Assert.Equal("New Cat",  result.Name);
            Assert.Equal("new-cat",  result.Slug);
        }

        [Fact]
        public async Task RenameCategory_ShouldThrow_WhenNameConflictsWithAnotherCategory()
        {
            _categories.Seed([new Category { Name = "Old", Slug = "old" }, new Category { Name = "Taken", Slug = "taken" }]);
            var cat = _db.Categories.First(c => c.Name == "Old");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.RenameCategory(cat.Id, "Taken"));
        }

        [Fact]
        public async Task RenameCategory_ShouldThrow_WhenCategoryNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.RenameCategory(9999, "Anything"));
        }

        // ── DeleteCategory ────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteCategory_ShouldRemoveCategory_WhenExists()
        {
            _categories.Seed([new Category { Name = "ToDelete", Slug = "to-delete" }]);
            var cat = _db.Categories.First();

            await _sut.DeleteCategory(cat.Id);

            Assert.Equal(0, _db.Categories.Count());
        }
    }
}
