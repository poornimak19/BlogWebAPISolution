using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Exceptions;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    public class CommentServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, Post>    _posts;
        private readonly InMemoryRepository<Guid, Comment> _comments;
        private readonly InMemoryRepository<Guid, User>    _users;
        private readonly CommentService _sut;

        public CommentServiceTests()
        {
            _db       = TestDbContextFactory.Create();
            _posts    = new InMemoryRepository<Guid, Post>(_db);
            _comments = new InMemoryRepository<Guid, Comment>(_db);
            _users    = new InMemoryRepository<Guid, User>(_db);
            _sut      = new CommentService(_posts, _comments, _users);
        }

        public void Dispose() => _db.Dispose();

        // ── Helpers ───────────────────────────────────────────────────────────

        private User MakeUser(bool canComment = true)
        {
            var u = new User { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString("N")[..8], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [], CanComment = canComment };
            _users.Seed([u]);
            return u;
        }

        private Post MakePost(bool commentsEnabled = true, bool autoApprove = true)
        {
            var authorId = Guid.NewGuid();
            var p = new Post
            {
                Id = Guid.NewGuid(), AuthorId = authorId,
                Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "html",
                CommentsEnabled = commentsEnabled, AutoApproveComments = autoApprove
            };
            _posts.Seed([p]);
            return p;
        }

        // ── Add ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task Add_ShouldCreateComment_WhenValidInput()
        {
            var user = MakeUser();
            var post = MakePost();

            var comment = await _sut.Add(post.Id, user.Id, "Hello!", null);

            Assert.NotNull(comment);
            Assert.Equal("Hello!", comment.Content);
            Assert.Equal(CommentStatus.Approved, comment.Status);
        }

        [Fact]
        public async Task Add_ShouldSetStatusPending_WhenAutoApproveIsFalse()
        {
            var user = MakeUser();
            var post = MakePost(autoApprove: false);

            var comment = await _sut.Add(post.Id, user.Id, "Pending comment", null);

            Assert.Equal(CommentStatus.Pending, comment.Status);
        }

        [Fact]
        public async Task Add_ShouldThrow_WhenUserNotFound()
        {
            var post = MakePost();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Add(post.Id, Guid.NewGuid(), "text", null));
        }

        [Fact]
        public async Task Add_ShouldThrow_WhenUserIsBannedFromCommenting()
        {
            var user = MakeUser(canComment: false);
            var post = MakePost();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Add(post.Id, user.Id, "text", null));
        }

        [Fact]
        public async Task Add_ShouldThrow_WhenPostNotFound()
        {
            var user = MakeUser();

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Add(Guid.NewGuid(), user.Id, "text", null));
        }

        [Fact]
        public async Task Add_ShouldThrow_WhenCommentsDisabled()
        {
            var user = MakeUser();
            var post = MakePost(commentsEnabled: false);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Add(post.Id, user.Id, "text", null));
        }

        [Fact]
        public async Task Add_ShouldThrow_WhenParentCommentBelongsToDifferentPost()
        {
            var user   = MakeUser();
            var post   = MakePost();
            var parent = new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "p", AuthorId = user.Id };
            _comments.Seed([parent]);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Add(post.Id, user.Id, "reply", parent.Id));
        }

        [Fact]
        public async Task Add_ShouldSetParentCommentId_WhenValidParent()
        {
            var user   = MakeUser();
            var post   = MakePost();
            var parent = new Comment { Id = Guid.NewGuid(), PostId = post.Id, Content = "parent", AuthorId = user.Id };
            _comments.Seed([parent]);

            var reply = await _sut.Add(post.Id, user.Id, "reply", parent.Id);

            Assert.Equal(parent.Id, reply.ParentCommentId);
        }

        // ── GetById ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ShouldReturnComment_WhenExists()
        {
            var comment = new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "hi", AuthorId = Guid.NewGuid() };
            _comments.Seed([comment]);

            var result = await _sut.GetById(comment.Id);

            Assert.NotNull(result);
            Assert.Equal(comment.Id, result!.Id);
        }

        [Fact]
        public async Task GetById_ShouldReturnNull_WhenNotFound()
        {
            var result = await _sut.GetById(Guid.NewGuid());
            Assert.Null(result);
        }

        // ── Update ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ShouldUpdateContent_WhenCalledByAuthor()
        {
            var authorId = Guid.NewGuid();
            var postAuthorId = Guid.NewGuid();
            var post     = new Post { Id = Guid.NewGuid(), AuthorId = postAuthorId, Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment  = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = authorId, Content = "old" };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await _sut.Update(comment.Id, authorId, "new content", null);

            var updated = await _db.Comments.FindAsync(comment.Id);
            Assert.Equal("new content", updated!.Content);
        }

        [Fact]
        public async Task Update_ShouldUpdateStatus_WhenCalledByPostAuthor()
        {
            var postAuthorId = Guid.NewGuid();
            var post         = new Post { Id = Guid.NewGuid(), AuthorId = postAuthorId, Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment      = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = Guid.NewGuid(), Content = "text" };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await _sut.Update(comment.Id, postAuthorId, "text", "Approved");

            var updated = await _db.Comments.FindAsync(comment.Id);
            Assert.Equal(CommentStatus.Approved, updated!.Status);
        }

        [Fact]
        public async Task Update_ShouldThrow_WhenNeitherAuthorNorPostAuthor()
        {
            var post    = new Post { Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = Guid.NewGuid(), Content = "text" };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.Update(comment.Id, Guid.NewGuid(), "hacked", null));
        }

        [Fact]
        public async Task Update_ShouldThrow_WhenCommentNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Update(Guid.NewGuid(), Guid.NewGuid(), "text", null));
        }

        // ── Delete ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ShouldRemoveComment_WhenCalledByAuthor()
        {
            var authorId = Guid.NewGuid();
            var post     = new Post { Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment  = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = authorId, Content = "text" };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await _sut.Delete(comment.Id, authorId);

            Assert.Null(await _db.Comments.FindAsync(comment.Id));
        }

        [Fact]
        public async Task Delete_ShouldThrow_WhenUnauthorized()
        {
            var post    = new Post { Id = Guid.NewGuid(), AuthorId = Guid.NewGuid(), Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = Guid.NewGuid(), Content = "text" };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.Delete(comment.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task Delete_ShouldThrow_WhenCommentNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.Delete(Guid.NewGuid(), Guid.NewGuid()));
        }

        // ── GetByPost ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByPost_ShouldReturnApprovedTopLevelComments()
        {
            var postId = Guid.NewGuid();
            _comments.Seed([
                new Comment { Id = Guid.NewGuid(), PostId = postId, Content = "a", Status = CommentStatus.Approved,  ParentCommentId = null },
                new Comment { Id = Guid.NewGuid(), PostId = postId, Content = "b", Status = CommentStatus.Pending,   ParentCommentId = null },
                new Comment { Id = Guid.NewGuid(), PostId = postId, Content = "c", Status = CommentStatus.Approved,  ParentCommentId = Guid.NewGuid() }
            ]);

            var (items, total) = await _sut.GetByPost(postId, 1, 10);

            Assert.Equal(1, total);
            Assert.Single(items);
        }

        // ── GetPendingComments ────────────────────────────────────────────────

        [Fact]
        public async Task GetPendingComments_ShouldReturnOnlyPendingNonDeleted()
        {
            _comments.Seed([
                new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "p", Status = CommentStatus.Pending,  IsDeleted = false },
                new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "a", Status = CommentStatus.Approved, IsDeleted = false },
                new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "d", Status = CommentStatus.Pending,  IsDeleted = true  }
            ]);

            var (items, total) = await _sut.GetPendingComments(1, 10);

            Assert.Equal(1, total);
        }

        // ── AdminApprove ──────────────────────────────────────────────────────

        [Fact]
        public async Task AdminApprove_ShouldSetStatusApproved()
        {
            var comment = new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "c", Status = CommentStatus.Pending };
            _comments.Seed([comment]);

            await _sut.AdminApprove(comment.Id);

            var updated = await _db.Comments.FindAsync(comment.Id);
            Assert.Equal(CommentStatus.Approved, updated!.Status);
        }

        [Fact]
        public async Task AdminApprove_ShouldThrow_WhenCommentNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.AdminApprove(Guid.NewGuid()));
        }

        // ── AdminDelete ───────────────────────────────────────────────────────

        [Fact]
        public async Task AdminDelete_ShouldSoftDeleteComment()
        {
            var commentId = Guid.NewGuid();
            var comment   = new Comment { Id = commentId, PostId = Guid.NewGuid(), Content = "c", Status = CommentStatus.Approved, IsDeleted = false };
            _comments.Seed([comment]);

            await _sut.AdminDelete(commentId);

            var updated = await _db.Comments.FindAsync(commentId);
            Assert.True(updated!.IsDeleted);
            Assert.Equal(CommentStatus.Removed, updated.Status);
        }

        [Fact]
        public async Task AdminDelete_ShouldThrow_WhenCommentNotFound()
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _sut.AdminDelete(Guid.NewGuid()));
        }

        // ── GetCommentStats ───────────────────────────────────────────────────

        [Fact]
        public async Task GetCommentStats_ShouldReturnCorrectTotals()
        {
            _comments.Seed([
                new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "a", Status = CommentStatus.Approved, IsDeleted = false },
                new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "b", Status = CommentStatus.Pending,  IsDeleted = false },
                new Comment { Id = Guid.NewGuid(), PostId = Guid.NewGuid(), Content = "c", Status = CommentStatus.Pending,  IsDeleted = true  }
            ]);

            var (total, pending) = await _sut.GetCommentStats();

            Assert.Equal(2, total);   // excludes deleted
            Assert.Equal(1, pending);
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class CommentServiceBranchTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, Post>    _posts;
        private readonly InMemoryRepository<Guid, Comment> _comments;
        private readonly InMemoryRepository<Guid, User>    _users;
        private readonly CommentService _sut;

        public CommentServiceBranchTests()
        {
            _db       = TestDbContextFactory.Create();
            _posts    = new InMemoryRepository<Guid, Post>(_db);
            _comments = new InMemoryRepository<Guid, Comment>(_db);
            _users    = new InMemoryRepository<Guid, User>(_db);
            _sut      = new CommentService(_posts, _comments, _users);
        }

        public void Dispose() => _db.Dispose();

        private User MakeUser()
        {
            var u = new User { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString("N")[..8], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [], CanComment = true };
            _users.Seed([u]);
            return u;
        }

        private Post MakePost(Guid? authorId = null)
        {
            var p = new Post { Id = Guid.NewGuid(), AuthorId = authorId ?? Guid.NewGuid(), Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "html", CommentsEnabled = true, AutoApproveComments = true };
            _posts.Seed([p]);
            return p;
        }

        // ── Add: parentCommentId provided but parent not found ────────────────

        [Fact]
        public async Task Add_ShouldThrow_WhenParentCommentNotFound()
        {
            var user = MakeUser();
            var post = MakePost();
            // Pass a parentCommentId that doesn't exist in DB
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Add(post.Id, user.Id, "reply", Guid.NewGuid()));
        }

        // ── Delete: comment with replies — replies deleted first ──────────────

        [Fact]
        public async Task Delete_ShouldDeleteReplies_BeforeDeletingParent()
        {
            var authorId = Guid.NewGuid();
            var post     = MakePost();
            var parent   = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = authorId, Content = "parent" };
            var reply    = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = authorId, Content = "reply", ParentCommentId = parent.Id };
            _comments.Seed([parent, reply]);

            await _sut.Delete(parent.Id, authorId);

            Assert.Null(await _db.Comments.FindAsync(parent.Id));
            Assert.Null(await _db.Comments.FindAsync(reply.Id));
        }

        [Fact]
        public async Task Delete_ShouldSucceed_WhenCommentHasNoReplies()
        {
            var authorId = Guid.NewGuid();
            var post     = MakePost();
            var comment  = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = authorId, Content = "no replies" };
            _comments.Seed([comment]);

            await _sut.Delete(comment.Id, authorId);

            Assert.Null(await _db.Comments.FindAsync(comment.Id));
        }

        // ── Delete: called by post author (not comment author) ────────────────

        [Fact]
        public async Task Delete_ShouldSucceed_WhenCalledByPostAuthor()
        {
            var postAuthorId = Guid.NewGuid();
            var post         = MakePost(postAuthorId);
            var comment      = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = Guid.NewGuid(), Content = "text" };
            _comments.Seed([comment]);

            await _sut.Delete(comment.Id, postAuthorId);

            Assert.Null(await _db.Comments.FindAsync(comment.Id));
        }

        // ── AdminDelete: with replies — all soft-deleted ──────────────────────

        [Fact]
        public async Task AdminDelete_ShouldSoftDeleteReplies_WhenParentHasReplies()
        {
            var parentId = Guid.NewGuid();
            var replyId  = Guid.NewGuid();
            var parent   = new Comment { Id = parentId, PostId = Guid.NewGuid(), Content = "parent", Status = CommentStatus.Approved, IsDeleted = false };
            var reply    = new Comment { Id = replyId,  PostId = Guid.NewGuid(), Content = "reply",  Status = CommentStatus.Approved, IsDeleted = false, ParentCommentId = parentId };
            _comments.Seed([parent, reply]);

            await _sut.AdminDelete(parentId);

            var updatedParent = await _db.Comments.FindAsync(parentId);
            var updatedReply  = await _db.Comments.FindAsync(replyId);
            Assert.True(updatedParent!.IsDeleted);
            Assert.True(updatedReply!.IsDeleted);
            Assert.Equal(CommentStatus.Removed, updatedReply.Status);
        }

        // ── Update: actor is both comment author AND post author ──────────────

        [Fact]
        public async Task Update_ShouldUpdateBothContentAndStatus_WhenActorIsBothAuthorAndPostAuthor()
        {
            var actorId = Guid.NewGuid();
            var post    = new Post { Id = Guid.NewGuid(), AuthorId = actorId, Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = actorId, Content = "old" };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await _sut.Update(comment.Id, actorId, "new content", "Approved");

            var updated = await _db.Comments.FindAsync(comment.Id);
            Assert.Equal("new content", updated!.Content);
            Assert.Equal(CommentStatus.Approved, updated.Status);
        }

        // ── Update: post author with null status (no status change) ──────────

        [Fact]
        public async Task Update_ShouldNotChangeStatus_WhenPostAuthorPassesNullStatus()
        {
            var postAuthorId = Guid.NewGuid();
            var post         = new Post { Id = Guid.NewGuid(), AuthorId = postAuthorId, Title = "T", Slug = Guid.NewGuid().ToString("N"), ContentHtml = "" };
            var comment      = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = Guid.NewGuid(), Content = "text", Status = CommentStatus.Pending };
            _posts.Seed([post]);
            _comments.Seed([comment]);

            await _sut.Update(comment.Id, postAuthorId, "text", null);

            var updated = await _db.Comments.FindAsync(comment.Id);
            Assert.Equal(CommentStatus.Pending, updated!.Status); // unchanged
        }

        // ── GetByPost: pagination ─────────────────────────────────────────────

        [Fact]
        public async Task GetByPost_ShouldPaginate_Correctly()
        {
            var postId = Guid.NewGuid();
            for (int i = 0; i < 5; i++)
                _comments.Seed([new Comment { Id = Guid.NewGuid(), PostId = postId, Content = $"c{i}", Status = CommentStatus.Approved, ParentCommentId = null }]);

            var (page1, total) = await _sut.GetByPost(postId, 1, 3);
            var (page2, _)     = await _sut.GetByPost(postId, 2, 3);

            Assert.Equal(5, total);
            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page2.Count);
        }

        // ── GetPendingComments: pagination ────────────────────────────────────

        [Fact]
        public async Task GetPendingComments_ShouldPaginate_Correctly()
        {
            using var db  = TestDbContextFactory.Create();
            var posts     = new InMemoryRepository<Guid, Post>(db);
            var comments  = new InMemoryRepository<Guid, Comment>(db);
            var sut       = new CommentService(posts, comments, new InMemoryRepository<Guid, User>(db));

            var postId = Guid.NewGuid();
            posts.Seed([new Post { Id = postId, AuthorId = Guid.NewGuid(), Title = "T", Slug = "s", ContentHtml = "" }]);

            for (int i = 0; i < 5; i++)
                comments.Seed([new Comment { Id = Guid.NewGuid(), PostId = postId, Content = $"p{i}", Status = CommentStatus.Pending, IsDeleted = false }]);

            var (page1, total) = await sut.GetPendingComments(1, 3);
            var (page2, _)     = await sut.GetPendingComments(2, 3);

            Assert.Equal(5, total);
            Assert.Equal(3, page1.Count);
            Assert.Equal(2, page2.Count);
        }

        // ── GetCommentStats: all zero ─────────────────────────────────────────

        [Fact]
        public async Task GetCommentStats_ShouldReturnZeros_WhenNoComments()
        {
            var (total, pending) = await _sut.GetCommentStats();
            Assert.Equal(0, total);
            Assert.Equal(0, pending);
        }
    }
}
