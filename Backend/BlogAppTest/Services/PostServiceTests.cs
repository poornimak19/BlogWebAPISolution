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
    public class PostServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, User> _users;
        private readonly InMemoryRepository<Guid, Post> _posts;
        private readonly Mock<IRepository<Guid, Tag>>      _tagsMock;
        private readonly Mock<IRepository<Guid, Category>> _categoriesMock;
        private readonly PostService _sut;

        public PostServiceTests()
        {
            _db             = TestDbContextFactory.Create();
            _users          = new InMemoryRepository<Guid, User>(_db);
            _posts          = new InMemoryRepository<Guid, Post>(_db);
            _tagsMock       = new Mock<IRepository<Guid, Tag>>();
            _categoriesMock = new Mock<IRepository<Guid, Category>>();

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

        // ── Helpers ───────────────────────────────────────────────────────────

        private User SeedUser(string? username = null)
        {
            var u = new User
            {
                Id = Guid.NewGuid(),
                Username = username ?? Guid.NewGuid().ToString("N")[..10],
                Email = $"{Guid.NewGuid():N}@t.com",
                Password = [], PasswordHash = []
            };
            _users.Seed([u]);
            return u;
        }

        private Post SeedPost(Guid authorId,
            PostStatus status = PostStatus.Draft,
            bool isRejected   = false,
            Visibility vis    = Visibility.Public,
            string? slug      = null,
            string? title     = null)
        {
            var p = new Post
            {
                Id = Guid.NewGuid(), AuthorId = authorId,
                Title = title ?? "Test Post",
                Slug  = slug  ?? "slug-" + Guid.NewGuid().ToString("N")[..6],
                ContentHtml = "<p>content</p>",
                Status = status, Visibility = vis, IsRejected = isRejected,
                CreatedAt = DateTime.UtcNow
            };
            _posts.Seed([p]);
            return p;
        }

        private Tag SeedTag(string slug)
        {
            var t = new Tag { Name = slug, Slug = slug };
            _db.Tags.Add(t); _db.SaveChanges();
            return t;
        }

        private Category SeedCategory(string slug)
        {
            var c = new Category { Name = slug, Slug = slug };
            _db.Categories.Add(c); _db.SaveChanges();
            return c;
        }

        // ── Create — basic ────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ShouldReturnPost_WithDraftStatus()
        {
            var user = SeedUser();
            var post = await _sut.Create(user.Id, "My Title", null, null, "<p>html</p>", null, "Public", null, null, null, null, "");
            Assert.Equal(PostStatus.Draft, post.Status);
            Assert.Equal("my-title", post.Slug);
        }

        [Fact]
        public async Task Create_ShouldUseProvidedSlug()
        {
            var user = SeedUser();
            var post = await _sut.Create(user.Id, "Title", "my custom slug", null, "html", null, "Public", null, null, null, null, "");
            Assert.Equal("my-custom-slug", post.Slug);
        }

        [Fact]
        public async Task Create_ShouldAppendSuffix_WhenSlugAlreadyExists()
        {
            var user = SeedUser();
            SeedPost(user.Id, slug: "test-post");
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
        public async Task Create_ShouldDefaultCommentsEnabled_WhenNull()
        {
            var user = SeedUser();
            var post = await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, null, null, null, "");
            Assert.True(post.CommentsEnabled);
        }

        [Fact]
        public async Task Create_ShouldDefaultAutoApproveComments_WhenNull()
        {
            var user = SeedUser();
            var post = await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, null, null, null, "");
            Assert.True(post.AutoApproveComments);
        }

        [Fact]
        public async Task Create_ShouldRespectCommentsEnabled_WhenFalse()
        {
            var user = SeedUser();
            var post = await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, null, false, null, "");
            Assert.False(post.CommentsEnabled);
        }

        [Fact]
        public async Task Create_ShouldRespectAutoApproveComments_WhenFalse()
        {
            var user = SeedUser();
            var post = await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, null, null, false, "");
            Assert.False(post.AutoApproveComments);
        }

        // ── Create — tags & categories ────────────────────────────────────────

        [Fact]
        public async Task Create_ShouldCreateNewTags_WhenTagsDontExist()
        {
            var user = SeedUser();
            await _sut.Create(user.Id, "T", null, null, "html", null, "Public", ["dotnet", "csharp"], null, null, null, "");
            Assert.Equal(2, _db.Tags.Count());
        }

        [Fact]
        public async Task Create_ShouldReuseExistingTags()
        {
            var user = SeedUser();
            SeedTag("dotnet");
            await _sut.Create(user.Id, "T", null, null, "html", null, "Public", ["dotnet"], null, null, null, "");
            Assert.Equal(1, _db.Tags.Count());
        }

        [Fact]
        public async Task Create_ShouldCreateNewCategories_WhenCategoriesDontExist()
        {
            var user = SeedUser();
            await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, ["Tech", "Science"], null, null, "");
            Assert.Equal(2, _db.Categories.Count());
        }

        [Fact]
        public async Task Create_ShouldReuseExistingCategories()
        {
            var user = SeedUser();
            SeedCategory("tech");
            await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, ["tech"], null, null, "");
            Assert.Equal(1, _db.Categories.Count());
        }

        [Fact]
        public async Task Create_ShouldSkipTags_WhenTagNamesIsNull()
        {
            var user = SeedUser();
            await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, null, null, null, "");
            Assert.Equal(0, _db.Tags.Count());
        }

        [Fact]
        public async Task Create_ShouldSkipCategories_WhenCategoryNamesIsNull()
        {
            var user = SeedUser();
            await _sut.Create(user.Id, "T", null, null, "html", null, "Public", null, null, null, null, "");
            Assert.Equal(0, _db.Categories.Count());
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
            Assert.Null(await _sut.GetById(Guid.NewGuid()));
        }

        // ── GetBySlug ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetBySlug_ShouldReturnPost_WhenSlugMatches()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            Assert.NotNull(await _sut.GetBySlug(post.Slug));
        }

        [Fact]
        public async Task GetBySlug_ShouldReturnNull_WhenSlugNotFound()
        {
            Assert.Null(await _sut.GetBySlug("no-such-slug"));
        }

        // ── Update — field branches ───────────────────────────────────────────

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
        public async Task Update_ShouldUpdateTitle()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, "New Title", null, null, null, null, null, null, null, null, null, null, "");
            Assert.Equal("New Title", result.Title);
        }

        [Fact]
        public async Task Update_ShouldUpdateSlug()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, "new slug", null, null, null, null, null, null, null, null, null, "");
            Assert.Equal("new-slug", result.Slug);
        }

        [Fact]
        public async Task Update_ShouldUpdateExcerpt()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, "new excerpt", null, null, null, null, null, null, null, null, "");
            Assert.Equal("new excerpt", result.Excerpt);
        }

        [Fact]
        public async Task Update_ShouldUpdateContentHtml()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, "<b>new</b>", null, null, null, null, null, null, null, "");
            Assert.Equal("<b>new</b>", result.ContentHtml);
        }

        [Fact]
        public async Task Update_ShouldUpdateContentMarkdown()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, null, "**new**", null, null, null, null, null, null, "");
            Assert.Equal("**new**", result.ContentMarkdown);
        }

        [Fact]
        public async Task Update_ShouldUpdateCommentsEnabled()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, null, null, false, null, null, "");
            Assert.False(result.CommentsEnabled);
        }

        [Fact]
        public async Task Update_ShouldUpdateAutoApproveComments()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, null, null, null, false, null, "");
            Assert.False(result.AutoApproveComments);
        }

        [Fact]
        public async Task Update_ShouldUpdateVisibility()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, null, null, "Private", null, null, null, null, null, "");
            Assert.Equal(Visibility.Private, result.Visibility);
        }

        [Fact]
        public async Task Update_ShouldUpdateStatus()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, null, null, null, null, "Published", "");
            Assert.Equal(PostStatus.Published, result.Status);
        }

        [Fact]
        public async Task Update_ShouldUpdateCoverImageUrl()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            var result = await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, null, null, null, null, null, "https://img.com/cover.jpg");
            Assert.Equal("https://img.com/cover.jpg", result.CoverImageUrl);
        }

        [Fact]
        public async Task Update_ShouldReplaceTags_WhenTagNamesProvided()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, ["newtag"], null, null, null, null, "");
            Assert.Equal(1, _db.Tags.Count());
        }

        [Fact]
        public async Task Update_ShouldReplaceCategories_WhenCategoryNamesProvided()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            await _sut.Update(post.Id, user.Id, null, null, null, null, null, null, null, ["newcat"], null, null, null, "");
            Assert.Equal(1, _db.Categories.Count());
        }

        [Fact]
        public async Task Update_ShouldNotChangeTags_WhenTagNamesIsNull()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            SeedTag("existing");
            await _sut.Update(post.Id, user.Id, "New Title", null, null, null, null, null, null, null, null, null, null, "");
            Assert.Equal(1, _db.Tags.Count()); // unchanged
        }

        // ── Publish ───────────────────────────────────────────────────────────

        [Fact]
        public async Task Publish_ShouldSetStatusDraft_AndClearRejected()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id, isRejected: true);
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
            var (_, total) = await _sut.GetPublished(1, 10, null, null, null, null);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetPublished_ShouldReturnPublicPosts_WhenNoCurrentUser()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published, vis: Visibility.Public);
            SeedPost(user.Id, PostStatus.Published, vis: Visibility.Private);
            var (_, total) = await _sut.GetPublished(1, 10, null, null, null, null);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetPublished_ShouldReturnPrivatePost_WhenCurrentUserIsAuthor()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published, vis: Visibility.Private);
            var (_, total) = await _sut.GetPublished(1, 10, null, null, null, user.Id);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetPublished_ShouldFilterByKeyword_InTitle()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published, title: "C# Tips");
            SeedPost(user.Id, PostStatus.Published, title: "Python Guide");
            var (_, total) = await _sut.GetPublished(1, 10, "C#", null, null, null);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetPublished_ShouldPaginate_Correctly()
        {
            var user = SeedUser();
            for (int i = 0; i < 5; i++)
                SeedPost(user.Id, PostStatus.Published);
            var (page1, total) = await _sut.GetPublished(1, 3, null, null, null, null);
            var (page2, _)     = await _sut.GetPublished(2, 3, null, null, null, null);
            Assert.Equal(5, total);
            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page2.Count);
        }

        [Fact]
        public async Task GetPublished_ShouldReturnEmpty_WhenNoPosts()
        {
            var (items, total) = await _sut.GetPublished(1, 10, null, null, null, null);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        // ── GetByAuthor ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetByAuthor_ShouldReturnOnlyAuthorPosts()
        {
            var u1 = SeedUser(); var u2 = SeedUser();
            SeedPost(u1.Id); SeedPost(u1.Id); SeedPost(u2.Id);
            var (_, total) = await _sut.GetByAuthor(u1.Id, 1, 10);
            Assert.Equal(2, total);
        }

        [Fact]
        public async Task GetByAuthor_ShouldReturnEmpty_WhenAuthorHasNoPosts()
        {
            var user = SeedUser();
            var (items, total) = await _sut.GetByAuthor(user.Id, 1, 10);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetByAuthor_ShouldPaginate_Correctly()
        {
            var user = SeedUser();
            for (int i = 0; i < 5; i++) SeedPost(user.Id);
            var (page1, total) = await _sut.GetByAuthor(user.Id, 1, 3);
            var (page2, _)     = await _sut.GetByAuthor(user.Id, 2, 3);
            Assert.Equal(5, total);
            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page2.Count);
        }

        // ── GetPendingPosts ───────────────────────────────────────────────────

        [Fact]
        public async Task GetPendingPosts_ShouldReturnDraftNonRejected()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Draft,     isRejected: false);
            SeedPost(user.Id, PostStatus.Draft,     isRejected: true);
            SeedPost(user.Id, PostStatus.Published, isRejected: false);
            var (_, total) = await _sut.GetPendingPosts(1, 10);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetPendingPosts_ShouldReturnEmpty_WhenNoPending()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published);
            var (items, total) = await _sut.GetPendingPosts(1, 10);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetPendingPosts_ShouldPaginate_Correctly()
        {
            var user = SeedUser();
            for (int i = 0; i < 5; i++) SeedPost(user.Id, PostStatus.Draft, isRejected: false);
            var (page1, total) = await _sut.GetPendingPosts(1, 3);
            var (page2, _)     = await _sut.GetPendingPosts(2, 3);
            Assert.Equal(5, total);
            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page2.Count);
        }

        // ── ApprovePost ───────────────────────────────────────────────────────

        [Fact]
        public async Task ApprovePost_ShouldSetPublishedAndClearRejected()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id, PostStatus.Draft, isRejected: true);
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
        public async Task AdminDelete_ShouldRemovePost()
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
            Assert.Equal(1, pending);
        }

        [Fact]
        public async Task GetPostStats_ShouldReturnZeros_WhenNoPosts()
        {
            var (total, published, draft, pending) = await _sut.GetPostStats();
            Assert.Equal(0, total);
            Assert.Equal(0, published);
            Assert.Equal(0, draft);
            Assert.Equal(0, pending);
        }

        // ── GetAllPosts ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllPosts_ShouldReturnAll_WhenNoFilters()
        {
            var user = SeedUser();
            SeedPost(user.Id, PostStatus.Published);
            SeedPost(user.Id, PostStatus.Draft);
            var (_, total) = await _sut.GetAllPosts(1, 10, null, null);
            Assert.Equal(2, total);
        }

        [Fact]
        public async Task GetAllPosts_ShouldFilterByVisibility_Public()
        {
            var user = SeedUser();
            SeedPost(user.Id, vis: Visibility.Public);
            SeedPost(user.Id, vis: Visibility.Private);
            var (_, total) = await _sut.GetAllPosts(1, 10, null, "Public");
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetAllPosts_ShouldFilterByVisibility_Private()
        {
            var user = SeedUser();
            SeedPost(user.Id, vis: Visibility.Public);
            SeedPost(user.Id, vis: Visibility.Private);
            var (_, total) = await _sut.GetAllPosts(1, 10, null, "Private");
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetAllPosts_ShouldIgnoreInvalidVisibility()
        {
            var user = SeedUser();
            SeedPost(user.Id); SeedPost(user.Id);
            var (_, total) = await _sut.GetAllPosts(1, 10, null, "InvalidValue");
            Assert.Equal(2, total); // no filter applied
        }

        [Fact]
        public async Task GetAllPosts_ShouldFilterBySearchQuery_InTitle()
        {
            var user = SeedUser();
            SeedPost(user.Id, title: "Unique XYZ Title");
            SeedPost(user.Id, title: "Other Post");
            var (_, total) = await _sut.GetAllPosts(1, 10, "XYZ", null);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetAllPosts_ShouldPaginate_Correctly()
        {
            var user = SeedUser();
            for (int i = 0; i < 5; i++) SeedPost(user.Id);
            var (page1, total) = await _sut.GetAllPosts(1, 3, null, null);
            var (page2, _)     = await _sut.GetAllPosts(2, 3, null, null);
            Assert.Equal(5, total);
            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page2.Count);
        }

        [Fact]
        public async Task GetAllPosts_ShouldReturnEmpty_WhenNoPosts()
        {
            var (items, total) = await _sut.GetAllPosts(1, 10, null, null);
            Assert.Equal(0, total);
            Assert.Empty(items);
        }
    }
}
