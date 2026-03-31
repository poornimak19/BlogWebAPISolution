using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;

namespace BlogAppTest.Services
{
    public class ReactionServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, PostLike>    _postLikes;
        private readonly InMemoryRepository<Guid, CommentLike> _commentLikes;
        private readonly ReactionService _sut;

        public ReactionServiceTests()
        {
            _db           = TestDbContextFactory.Create();
            _postLikes    = new InMemoryRepository<Guid, PostLike>(_db);
            _commentLikes = new InMemoryRepository<Guid, CommentLike>(_db);
            _sut          = new ReactionService(_postLikes, _commentLikes);
        }

        public void Dispose() => _db.Dispose();

        private User SeedUser()
        {
            var u = new User { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString("N")[..10], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _db.Users.Add(u);
            _db.SaveChanges();
            return u;
        }

        private Post SeedPost(Guid authorId)
        {
            var p = new Post { Id = Guid.NewGuid(), AuthorId = authorId, Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            _db.Posts.Add(p);
            _db.SaveChanges();
            return p;
        }

        // ── TogglePostLike ────────────────────────────────────────────────────

        [Fact]
        public async Task TogglePostLike_ShouldAddLike_WhenNotAlreadyLiked()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);

            var (liked, total) = await _sut.TogglePostLike(post.Id, user.Id);

            Assert.True(liked);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task TogglePostLike_ShouldRemoveLike_WhenAlreadyLiked()
        {
            var user = SeedUser();
            var post = SeedPost(user.Id);
            _db.PostLikes.Add(new PostLike { PostId = post.Id, UserId = user.Id });
            _db.SaveChanges();

            var (liked, total) = await _sut.TogglePostLike(post.Id, user.Id);

            Assert.False(liked);
            Assert.Equal(0, total);
        }

        [Fact]
        public async Task TogglePostLike_ShouldReturnCorrectCount_WhenMultipleLikesExist()
        {
            var author = SeedUser();
            var post   = SeedPost(author.Id);
            var user1  = SeedUser();
            var user2  = SeedUser();
            var newUser = SeedUser();

            _db.PostLikes.AddRange(
                new PostLike { PostId = post.Id, UserId = user1.Id },
                new PostLike { PostId = post.Id, UserId = user2.Id }
            );
            _db.SaveChanges();

            var (liked, total) = await _sut.TogglePostLike(post.Id, newUser.Id);

            Assert.True(liked);
            Assert.Equal(3, total);
        }

        // ── ToggleCommentLike ─────────────────────────────────────────────────

        [Fact]
        public async Task ToggleCommentLike_ShouldAddLike_WhenNotAlreadyLiked()
        {
            var user    = SeedUser();
            var post    = SeedPost(user.Id);
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = user.Id, Content = "c" };
            _db.Comments.Add(comment);
            _db.SaveChanges();

            var (liked, total) = await _sut.ToggleCommentLike(comment.Id, user.Id);

            Assert.True(liked);
            Assert.Equal(1, total);
        }

        [Fact]
        public async Task ToggleCommentLike_ShouldRemoveLike_WhenAlreadyLiked()
        {
            var user    = SeedUser();
            var post    = SeedPost(user.Id);
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = user.Id, Content = "c" };
            _db.Comments.Add(comment);
            _db.CommentLikes.Add(new CommentLike { CommentId = comment.Id, UserId = user.Id });
            _db.SaveChanges();

            var (liked, total) = await _sut.ToggleCommentLike(comment.Id, user.Id);

            Assert.False(liked);
            Assert.Equal(0, total);
        }
    }
}
