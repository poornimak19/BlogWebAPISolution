using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, User>   _users;
        private readonly InMemoryRepository<Guid, Follow> _follows;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _db      = TestDbContextFactory.Create();
            _users   = new InMemoryRepository<Guid, User>(_db);
            _follows = new InMemoryRepository<Guid, Follow>(_db);
            _sut     = new UserService(_users, _follows);
        }

        public void Dispose() => _db.Dispose();

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

        // ── GetByUsername ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetByUsername_ShouldReturnUser_WhenExists()
        {
            var user = SeedUser("alice");

            var result = await _sut.GetByUsername("alice");

            Assert.NotNull(result);
            Assert.Equal("alice", result!.Username);
        }

        [Fact]
        public async Task GetByUsername_ShouldReturnNull_WhenNotFound()
        {
            var result = await _sut.GetByUsername("ghost");
            Assert.Null(result);
        }

        // ── GetById ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ShouldReturnUser_WhenExists()
        {
            var user = SeedUser();

            var result = await _sut.GetById(user.Id);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
        }

        [Fact]
        public async Task GetById_ShouldReturnNull_WhenNotFound()
        {
            var result = await _sut.GetById(Guid.NewGuid());
            Assert.Null(result);
        }

        // ── UpdateProfile ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateProfile_ShouldUpdateFields_WhenUserExists()
        {
            var user = SeedUser();

            await _sut.UpdateProfile(user.Id, "New Name", "My bio", "http://avatar.png");

            var updated = await _db.Users.FindAsync(user.Id);
            Assert.Equal("New Name",          updated!.DisplayName);
            Assert.Equal("My bio",            updated.Bio);
            Assert.Equal("http://avatar.png", updated.AvatarUrl);
        }

        [Fact]
        public async Task UpdateProfile_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.UpdateProfile(Guid.NewGuid(), "name", null, null));
        }

        [Fact]
        public async Task UpdateProfile_ShouldNotOverwriteNullFields()
        {
            var user = SeedUser();
            var u = await _db.Users.FindAsync(user.Id);
            u!.DisplayName = "Old";
            u.Bio = "OldBio";
            await _db.SaveChangesAsync();

            await _sut.UpdateProfile(user.Id, null, null, null);

            var updated = await _db.Users.FindAsync(user.Id);
            Assert.Equal("Old",    updated!.DisplayName);
            Assert.Equal("OldBio", updated.Bio);
        }

        // ── GetFollowCounts ───────────────────────────────────────────────────

        [Fact]
        public async Task GetFollowCounts_ShouldReturnCorrectCounts()
        {
            var user1 = SeedUser();
            var user2 = SeedUser();
            var user3 = SeedUser();
            var userId = user1.Id;

            _follows.Seed([
                new Follow { FollowerId = user2.Id, FolloweeId = userId },
                new Follow { FollowerId = user3.Id, FolloweeId = userId },
                new Follow { FollowerId = userId,   FolloweeId = user2.Id }
            ]);

            var (followers, following) = await _sut.GetFollowCounts(userId);

            Assert.Equal(2, followers);
            Assert.Equal(1, following);
        }

        // ── SearchUsers ───────────────────────────────────────────────────────

        [Fact]
        public async Task SearchUsers_ShouldReturnMatchingUsers()
        {
            SeedUser("alice_dev");
            SeedUser("bob");

            var result = await _sut.SearchUsers("alice");

            Assert.Single(result);
        }

        // ── ChangeRole ────────────────────────────────────────────────────────

        [Fact]
        public async Task ChangeRole_ShouldUpdateRole_WhenUserExists()
        {
            var user = SeedUser();

            await _sut.ChangeRole(user.Id, UserRole.Blogger);

            var updated = await _db.Users.FindAsync(user.Id);
            Assert.Equal(UserRole.Blogger, updated!.Role);
        }

        [Fact]
        public async Task ChangeRole_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ChangeRole(Guid.NewGuid(), UserRole.Admin));
        }

        // ── SuspendUser ───────────────────────────────────────────────────────

        [Fact]
        public async Task SuspendUser_ShouldSetIsSuspended_WhenUserExists()
        {
            var user = SeedUser();

            await _sut.SuspendUser(user.Id, true);

            var updated = await _db.Users.FindAsync(user.Id);
            Assert.True(updated!.IsSuspended);
        }

        [Fact]
        public async Task SuspendUser_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.SuspendUser(Guid.NewGuid(), true));
        }

        // ── DeleteUser ────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_ShouldRemoveUser_WhenExists()
        {
            var user = SeedUser();

            await _sut.DeleteUser(user.Id);

            Assert.Null(await _db.Users.FindAsync(user.Id));
        }

        [Fact]
        public async Task DeleteUser_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.DeleteUser(Guid.NewGuid()));
        }

        // ── SetCommentPermission ──────────────────────────────────────────────

        [Fact]
        public async Task SetCommentPermission_ShouldUpdateCanComment_WhenUserExists()
        {
            var user = SeedUser();

            await _sut.SetCommentPermission(user.Id, false);

            var updated = await _db.Users.FindAsync(user.Id);
            Assert.False(updated!.CanComment);
        }

        [Fact]
        public async Task SetCommentPermission_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.SetCommentPermission(Guid.NewGuid(), false));
        }
    }
}
