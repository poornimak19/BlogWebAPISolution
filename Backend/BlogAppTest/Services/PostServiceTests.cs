using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;
using Moq;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    /// <summary>
    /// PostService uses IRepository&lt;Guid, Tag&gt; and IRepository&lt;Guid, Category&gt;
    /// (production mismatch — Tag/Category have int PKs but service declares Guid key).
    /// We use Moq for those two repos and EF InMemory for Users/Posts.
    /// </summary>
    public class PostServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, User> _users;
        private readonly InMemoryRepository<Guid, Post> _posts;
        private readonly Mock<IRepository<Guid, Tag>>      _tagsMock;
        private readonly Mock<IRepository<Guid, Category>> _categoriesMock;
        private readonly List<Tag>      _tagList      = [];
        private readonly List<Category> _categoryList = [];
        private readonly PostService _sut;

        public PostServiceTests()
        {
            _db             = TestDbContextFactory.Create();
            _users          = new InMemoryRepository<Guid, User>(_db);
            _posts          = new InMemoryRepository<Guid, Post>(_db);
            _tagsMock       = new Mock<IRepository<Guid, Tag>>();
            _categoriesMock = new Mock<IRepository<Guid, Category>>();

            // EF-backed queryable for tags/categories via the same DbContext
            _tagsMock.Setup(r => r.GetQueryable()).Returns(() => _db.Tags.AsQueryable());
            _categoriesMock.Setup(r => r.GetQueryable()).Returns(() => _db.Categories.AsQueryable());

            _tagsMock.Setup(r => r.Add(It.IsAny<Tag>()))
                     .Callback<Tag>(t => { _db.Tags.Add(t); _db.SaveChanges(); })
                     .ReturnsAsync((Tag t) => t);

            _categoriesMock.Setup(r => r.Add(It.IsAny<Category>()))
                           .Callback<Category>(c => { _db.Categories.Add(c); _db.SaveChanges(); })
                           .ReturnsAsync((Category c) => c);

            _sut = new PostService(_users, _posts, _tagsMock.Object, _categoriesMock.Object);
        }

        public void Dispose() => _db.Dispose();

        private User SeedUser()
        {
            var u = new User { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString("N")[..10], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _users.Seed([u]);
            return u;
        }

        private Post SeedPost(Guid authorId, PostStatus status = PostStatus.Draft, bool isRejected = false, Visibility visibility = Visibility.Public)
        {
            var p = new Post
            {
                Id          = Guid.NewGuid(),
                AuthorId    = authorId,
                Title       = "Test Post",
                Slug        = "test-post-" + Guid.NewGuid().ToString("N")[..6],
                ContentHtml = "<p>content</p>",
                Status      = status,
                Visibility  = visibility,
                IsRejected  = isRejected
            };
            _posts.Seed([p]);
            return p;
        }

        // ── Create ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ShouldReturnPost_WhenAuthorExists()
        {
            var user = SeedUser();

            var post = await _sut.Create(user.Id, "My Title", null, null, "<p>html</p>", null, "Public", null, null, null, null, "");

            Assert.NotNull(post);
            Assert.Equal("my-title", post.Slug);
            Assert.Equal(PostStatus.Draft, post.Status);
        }

        [Fact]
        public async Task Create_ShouldUseProvidedSlug_WhenSlugSupplied()
        {
            var user = SeedUser();

            // Slugify converts "my custom slug" → "my-custom-slug"
            var post = await _sut.Create(user.Id, "Title", "my custom slug", null, "html", null, "Public", null, null, null, null, "");

            Assert.Equal("my-custom-slug", post.Slug);
        }

        [Fact]
        public async Task Create_ShouldAppendSuffix_WhenSlugAlreadyExists()
        {
            var user = SeedUser();
            // Seed a post with slug "test-post"
            _posts.Seed([new Post { Id = Guid.NewGuid(), AuthorId = user.Id, Title = "Test Post", Slug = "test-post", ContentHtml = "" }]);

            var post = await _sut.Create(user.Id, "Test Post", null, null, "html", null, "Public", null, null, null, null, "");

            Assert.StartsWith("test-post-", post.Slug);
        }

        [Fact]
        public async Task Create_ShouldThrow_WhenAuthorNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Create(Guid.NewGuid(), "Title", null, null, "html", null, "Public", null, null, null, null, ""));
        }

        [Fact]
        public async Task Create_ShouldCreateNewTags_WhenTagsDontExist()
        {
            var user = SeedUser();

            await _sut.Create(user.Id, "Title", null, null, "html", null, "Public", ["dotnet", "csharp"], null, null, null, "");

            Assert.Equal(2, _db.Tags.Count());
        }

        [Fact]
        public async Task Create_ShouldReuseExistingTags_WhenTagsAlreadyExist()
        {
            var user = SeedUser();
            _db.Tags.Add(new Tag { Name = "dotnet", Slug = "dotnet" });
            _db.SaveChanges();

            await _sut.Create(user.Id, "Title", null, null, "html", null, "Public", ["dotnet"], null, null, null, "");

            Assert.Equal(1, _db.Tags.Count()); // no new tag created
        }

        [Fact]
        public async Task Create_ShouldCreateNewCategories_WhenCategoriesDontExist()
        {
            var user = SeedUser();

            await _sut.Create(user.Id, "Title", null, null, "html", null, "Public", null, ["Tech", "Science"], null, null, "");

            Assert.Equal(2, _db.Categories.Count());
        }

        // ── GetById ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ShouldReturnPost_WhenExists()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            var result = await _sut.GetById(post.Id);

            Assert.NotNull(result);
            Assert.Equal(post.Id, result!.Id);
        }

        [Fact]
        public async Task GetById_ShouldReturnNull_WhenNotFound()
        {
            var result = await _sut.GetById(Guid.NewGuid());
            Assert.Null(result);
        }

        // ── GetBySlug ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetBySlug_ShouldReturnPost_WhenSlugMatches()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            var result = await _sut.GetBySlug(post.Slug);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetBySlug_ShouldReturnNull_WhenSlugNotFound()
        {
            var result = await _sut.GetBySlug("no-such-slug");
            Assert.Null(result);
        }

        // ── Update ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ShouldUpdateTitle_WhenCalledByAuthor()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            var updated = await _sut.Update(post.Id, user.Id, "New Title", null, null, null, null, null, null, null, null, null, null, "");

            Assert.Equal("New Title", updated.Title);
        }

        [Fact]
        public async Task Update_ShouldThrow_WhenPostNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Update(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, null, null, null, null, null, null, ""));
        }

        [Fact]
        public async Task Update_ShouldThrow_WhenCalledByNonAuthor()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.Update(post.Id, Guid.NewGuid(), "Hacked", null, null, null, null, null, null, null, null, null, null, ""));
        }

        [Fact]
        public async Task Update_ShouldUpdateVisibility_WhenProvided()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            var updated = await _sut.Update(post.Id, user.Id, null, null, null, null, null, "Private", null, null, null, null, null, "");

            Assert.Equal(Visibility.Private, updated.Visibility);
        }

        [Fact]
        public async Task Update_ShouldUpdateStatus_WhenProvided()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            var updated = await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, null, null, null, null, "Published", "");

            Assert.Equal(PostStatus.Published, updated.Status);
        }

        // ── Publish ───────────────────────────────────────────────────────────

        [Fact]
        public async Task Publish_ShouldSetStatusDraft_WhenCalledByAuthor()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            await _sut.Publish(post.Id, user.Id);

            var updated = await _db.Posts.FindAsync(post.Id);
            Assert.Equal(PostStatus.Draft, updated!.Status);
            Assert.False(updated.IsRejected);
        }

        [Fact]
        public async Task Publish_ShouldThrow_WhenPostNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Publish(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task Publish_ShouldThrow_WhenCalledByNonAuthor()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.Publish(post.Id, Guid.NewGuid()));
        }

        // ── Delete ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ShouldRemovePost_WhenCalledByAuthor()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            await _sut.Delete(post.Id, user.Id);

            Assert.Null(await _db.Posts.FindAsync(post.Id));
        }

        [Fact]
        public async Task Delete_ShouldThrow_WhenPostNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Delete(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task Delete_ShouldThrow_WhenCalledByNonAuthor()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.Delete(post.Id, Guid.NewGuid()));
        }

        // ── GetPublished ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetPublished_ShouldReturnOnlyPublishedPosts()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published);
            SeedPost(user.Id, PostStatus.Draft);

            var (items, total) = await _sut.GetPublished(1, 10, null, null, null, null);

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetPublished_ShouldFilterByKeyword()
        {
            var user = SeedUser();
            var p    = SeedPost(user.Id, PostStatus.Published);
            // Update title directly in DB
            var dbPost = await _db.Posts.FindAsync(p.Id);
            dbPost!.Title = "C# Tips";
            await _db.SaveChangesAsync();

            var (items, total) = await _sut.GetPublished(1, 10, "C#", null, null, null);

            Assert.Equal(1, total);
        }

        // ── GetByAuthor ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetByAuthor_ShouldReturnOnlyAuthorPosts()
        {
            var user1 = SeedUser();
            var user2 = SeedUser();
            SeedPost(user1.Id);
            SeedPost(user1.Id);
            SeedPost(user2.Id);

            var (items, total) = await _sut.GetByAuthor(user1.Id, 1, 10);

            Assert.Equal(2, total);
        }

        // ── GetPendingPosts ───────────────────────────────────────────────────

        [Fact]
        public async Task GetPendingPosts_ShouldReturnDraftNonRejectedPosts()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Draft,     isRejected: false);
            SeedPost(user.Id, PostStatus.Draft,     isRejected: true);
            SeedPost(user.Id, PostStatus.Published, isRejected: false);

            var (items, total) = await _sut.GetPendingPosts(1, 10);

            Assert.Equal(1, total);
        }

        // ── ApprovePost ───────────────────────────────────────────────────────

        [Fact]
        public async Task ApprovePost_ShouldSetStatusPublished()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id, PostStatus.Draft);

            await _sut.ApprovePost(post.Id);

            var updated = await _db.Posts.FindAsync(post.Id);
            Assert.Equal(PostStatus.Published, updated!.Status);
            Assert.False(updated.IsRejected);
            Assert.NotNull(updated.PublishedAt);
        }

        [Fact]
        public async Task ApprovePost_ShouldThrow_WhenPostNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ApprovePost(Guid.NewGuid()));
        }

        // ── RejectPost ────────────────────────────────────────────────────────

        [Fact]
        public async Task RejectPost_ShouldSetIsRejectedTrue()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id, PostStatus.Draft);

            await _sut.RejectPost(post.Id);

            var updated = await _db.Posts.FindAsync(post.Id);
            Assert.True(updated!.IsRejected);
            Assert.Equal(PostStatus.Draft, updated.Status);
        }

        [Fact]
        public async Task RejectPost_ShouldThrow_WhenPostNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.RejectPost(Guid.NewGuid()));
        }

        // ── AdminDelete ───────────────────────────────────────────────────────

        [Fact]
        public async Task AdminDelete_ShouldRemovePost_WhenExists()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            await _sut.AdminDelete(post.Id);

            Assert.Null(await _db.Posts.FindAsync(post.Id));
        }

        [Fact]
        public async Task AdminDelete_ShouldThrow_WhenPostNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.AdminDelete(Guid.NewGuid()));
        }

        // ── GetPostStats ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetPostStats_ShouldReturnCorrectCounts()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published);
            SeedPost(user.Id, PostStatus.Draft, isRejected: false);
            SeedPost(user.Id, PostStatus.Draft, isRejected: true);

            var (total, published, draft, pending) = await _sut.GetPostStats();

            Assert.Equal(3, total);
            Assert.Equal(1, published);
            Assert.Equal(1, draft);
        }

        // ── GetAllPosts ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllPosts_ShouldReturnAllPosts_WhenNoFilters()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published);
            SeedPost(user.Id, PostStatus.Draft);

            var (items, total) = await _sut.GetAllPosts(1, 10, null, null);

            Assert.Equal(2, total);
        }

        [Fact]
        public async Task GetAllPosts_ShouldFilterByVisibility()
        {
            var user = SeedUser();
            SeedPost(user.Id, visibility: Visibility.Public);
            SeedPost(user.Id, visibility: Visibility.Private);

            var (items, total) = await _sut.GetAllPosts(1, 10, null, "Private");

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetAllPosts_ShouldFilterBySearchQuery()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var dbPost = await _db.Posts.FindAsync(post.Id);
            dbPost!.Title = "Unique Title XYZ";
            await _db.SaveChangesAsync();

            SeedPost(user.Id); // another post without XYZ

            var (items, total) = await _sut.GetAllPosts(1, 10, "XYZ", null);

            Assert.Equal(1, total);
        }
    }
}
