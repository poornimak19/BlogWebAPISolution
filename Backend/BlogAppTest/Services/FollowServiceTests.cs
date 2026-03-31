using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;

namespace BlogAppTest.Services
{
    public class FollowServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, Follow> _follows;
        private readonly FollowService _sut;

        public FollowServiceTests()
        {
            _db      = TestDbContextFactory.Create();
            _follows = new InMemoryRepository<Guid, Follow>(_db);
            _sut     = new FollowService(_follows);
        }

        public void Dispose() => _db.Dispose();

        private User SeedUser()
        {
            var u = new User { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString("N")[..10], Email = $"{Guid.NewGuid():N}@t.com", Password = [], PasswordHash = [] };
            _db.Users.Add(u);
            _db.SaveChanges();
            return u;
        }

        // ── ToggleFollow ──────────────────────────────────────────────────────

        [Fact]
        public async Task ToggleFollow_ShouldAddFollow_WhenNotAlreadyFollowing()
        {
            var follower = SeedUser();
            var followee = SeedUser();

            var (following, count) = await _sut.ToggleFollow(follower.Id, followee.Id);

            Assert.True(following);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task ToggleFollow_ShouldRemoveFollow_WhenAlreadyFollowing()
        {
            var follower = SeedUser();
            var followee = SeedUser();
            _follows.Seed([new Follow { FollowerId = follower.Id, FolloweeId = followee.Id }]);

            var (following, count) = await _sut.ToggleFollow(follower.Id, followee.Id);

            Assert.False(following);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task ToggleFollow_ShouldThrow_WhenFollowingSelf()
        {
            var id = Guid.NewGuid();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ToggleFollow(id, id));
        }

        // ── GetCounts ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetCounts_ShouldReturnCorrectFollowerAndFollowingCounts()
        {
            var user1 = SeedUser();
            var user2 = SeedUser();
            var user3 = SeedUser();

            _follows.Seed([
                new Follow { FollowerId = user2.Id, FolloweeId = user1.Id },
                new Follow { FollowerId = user3.Id, FolloweeId = user1.Id },
                new Follow { FollowerId = user1.Id, FolloweeId = user2.Id }
            ]);

            var (followers, following) = await _sut.GetCounts(user1.Id);

            Assert.Equal(2, followers);
            Assert.Equal(1, following);
        }

        [Fact]
        public async Task GetCounts_ShouldReturnZeros_WhenNoFollows()
        {
            var (followers, following) = await _sut.GetCounts(Guid.NewGuid());
            Assert.Equal(0, followers);
            Assert.Equal(0, following);
        }
    }
}
