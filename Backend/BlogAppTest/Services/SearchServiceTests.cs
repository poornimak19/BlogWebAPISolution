using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    public class SearchServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, Post> _posts;
        private readonly SearchService _sut;

        public SearchServiceTests()
        {
            _db    = TestDbContextFactory.Create();
            _posts = new InMemoryRepository<Guid, Post>(_db);
            _sut   = new SearchService(_posts);
        }

        public void Dispose() => _db.Dispose();

        private Post PublishedPost(string title, string content = "", string authorUsername = "alice",
                                   string tagSlug = "", string catSlug = "")
        {
            var author = new User { Id = Guid.NewGuid(), Username = authorUsername + Guid.NewGuid().ToString("N")[..4], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _db.Users.Add(author);
            _db.SaveChanges();

            var post = new Post
            {
                Id          = Guid.NewGuid(),
                AuthorId    = author.Id,
                Title       = title,
                Slug        = title.ToLower().Replace(" ", "-") + Guid.NewGuid().ToString("N")[..4],
                ContentHtml = content,
                Status      = PostStatus.Published,
                Visibility  = Visibility.Public
            };

            if (!string.IsNullOrEmpty(tagSlug))
            {
                var tag = new Tag { Name = tagSlug, Slug = tagSlug };
                _db.Tags.Add(tag);
                _db.SaveChanges();
                post.PostTags = [new PostTag { TagId = tag.Id }];
            }

            if (!string.IsNullOrEmpty(catSlug))
            {
                var cat = new Category { Name = catSlug, Slug = catSlug };
                _db.Categories.Add(cat);
                _db.SaveChanges();
                post.PostCategories = [new PostCategory { CategoryId = cat.Id }];
            }

            _posts.Seed([post]);
            return post;
        }

        [Fact]
        public async Task SearchPosts_ShouldReturnPublishedPublicPosts()
        {
            PublishedPost("Hello World");
            var author2 = new User { Id = Guid.NewGuid(), Username = "x" + Guid.NewGuid().ToString("N")[..4], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _db.Users.Add(author2);
            _db.SaveChanges();
            _posts.Seed([new Post { Id = Guid.NewGuid(), AuthorId = author2.Id, Title = "Draft", Slug = "draft" + Guid.NewGuid().ToString("N")[..4], ContentHtml = "", Status = PostStatus.Draft, Visibility = Visibility.Public }]);

            var (items, total) = await _sut.SearchPosts(1, 10, null, null, null, null, null);

            Assert.Equal(1, total);
            Assert.Equal("Hello World", items[0].Title);
        }

        [Fact]
        public async Task SearchPosts_ShouldFilterByKeyword()
        {
            PublishedPost("C# Tips");
            PublishedPost("Python Guide");

            var (items, total) = await _sut.SearchPosts(1, 10, "C#", null, null, null, null);

            Assert.Equal(1, total);
            Assert.Equal("C# Tips", items[0].Title);
        }

        [Fact]
        public async Task SearchPosts_ShouldFilterByTagSlug()
        {
            PublishedPost("Tagged Post", tagSlug: "dotnet");
            PublishedPost("No Tag");

            var (items, total) = await _sut.SearchPosts(1, 10, null, "dotnet", null, null, null);

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task SearchPosts_ShouldFilterByCategorySlug()
        {
            PublishedPost("Cat Post", catSlug: "tech");
            PublishedPost("No Cat");

            var (items, total) = await _sut.SearchPosts(1, 10, null, null, "tech", null, null);

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task SearchPosts_ShouldFilterByAuthorUsername()
        {
            var author = new User { Id = Guid.NewGuid(), Username = "uniqueauthor", Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _db.Users.Add(author);
            _db.SaveChanges();
            _posts.Seed([new Post { Id = Guid.NewGuid(), AuthorId = author.Id, Title = "Alice Post", Slug = "alice-post", ContentHtml = "", Status = PostStatus.Published, Visibility = Visibility.Public }]);
            PublishedPost("Bob Post");

            var (items, total) = await _sut.SearchPosts(1, 10, null, null, null, "uniqueauthor", null);

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task SearchPosts_ShouldRespectPagination()
        {
            PublishedPost("Post 1");
            PublishedPost("Post 2");
            PublishedPost("Post 3");

            var (items, total) = await _sut.SearchPosts(1, 2, null, null, null, null, null);

            Assert.Equal(3, total);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task SearchPosts_ShouldReturnEmpty_WhenNoMatches()
        {
            PublishedPost("Hello");

            var (items, total) = await _sut.SearchPosts(1, 10, "zzznomatch", null, null, null, null);

            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        [Fact]
        public async Task SearchPosts_ShouldOrderByPopular_WhenSortIsPopular()
        {
            var popular = PublishedPost("Popular");
            var user1   = new User { Id = Guid.NewGuid(), Username = "u1" + Guid.NewGuid().ToString("N")[..4], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            var user2   = new User { Id = Guid.NewGuid(), Username = "u2" + Guid.NewGuid().ToString("N")[..4], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _db.Users.AddRange(user1, user2);
            _db.SaveChanges();
            _db.PostLikes.AddRange(
                new PostLike { PostId = popular.Id, UserId = user1.Id },
                new PostLike { PostId = popular.Id, UserId = user2.Id }
            );
            _db.SaveChanges();

            PublishedPost("Less Popular");

            var (items, _) = await _sut.SearchPosts(1, 10, null, null, null, null, "popular");

            Assert.Equal("Popular", items[0].Title);
        }
    }
}
