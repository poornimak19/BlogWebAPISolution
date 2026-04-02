using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;
using Moq;
using System.Security.Cryptography;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, User> _users;
        private readonly Mock<IPasswordService> _pwdMock;
        private readonly Mock<ITokenService>    _tokenMock;
        private readonly AuthService            _sut;

        // Fixed fake bytes so password comparisons are deterministic
        private static readonly byte[] FakeHash = [1, 2, 3, 4];
        private static readonly byte[] FakeKey  = [5, 6, 7, 8];
        private const string FakeToken = "fake.jwt.token";
        private const string FakeResetToken = "fake.reset.token";

        public AuthServiceTests()
        {
            _db        = TestDbContextFactory.Create();
            _users     = new InMemoryRepository<Guid, User>(_db);
            _pwdMock   = new Mock<IPasswordService>();
            _tokenMock = new Mock<ITokenService>();

            // Default: HashPassword always returns FakeHash and outputs FakeKey
            _pwdMock.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]?>(), out It.Ref<byte[]?>.IsAny))
                    .Callback(new HashPasswordCallback((string _, byte[]? _, out byte[]? key) => key = FakeKey))
                    .Returns(FakeHash);

            _tokenMock.Setup(t => t.CreateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                      .Returns(FakeToken);

            _sut = new AuthService(_db, _pwdMock.Object, _tokenMock.Object, _users);
        }

        public void Dispose() => _db.Dispose();

        // Moq callback delegate for out-param
        private delegate void HashPasswordCallback(string password, byte[]? existingKey, out byte[]? hashKey);

        // ── Helpers ───────────────────────────────────────────────────────────

        private User SeedUser(
            string email    = "user@test.com",
            string username = "testuser",
            bool suspended  = false,
            UserRole role   = UserRole.Reader)
        {
            var user = new User
            {
                Id           = Guid.NewGuid(),
                Email        = email,
                Username     = username,
                Password     = FakeHash,
                PasswordHash = FakeKey,
                Role         = role,
                Status       = "active",
                IsSuspended  = suspended,
                CreatedAt    = DateTime.UtcNow
            };
            _users.Seed([user]);
            return user;
        }

        // ── Register ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_ShouldReturnUserAndToken_WhenValidInput()
        {
            var (user, token) = await _sut.Register("new@test.com", "newuser", "pass123", "New User", UserRole.Reader);

            Assert.NotNull(user);
            Assert.Equal("new@test.com", user.Email);
            Assert.Equal("newuser",      user.Username);
            Assert.Equal("New User",     user.DisplayName);
            Assert.Equal(UserRole.Reader, user.Role);
            Assert.Equal(FakeToken, token);
        }

        [Fact]
        public async Task Register_ShouldTrimEmailAndUsername()
        {
            var (user, _) = await _sut.Register("  trim@test.com  ", "  trimuser  ", "pass", null, UserRole.Reader);

            Assert.Equal("trim@test.com", user.Email);
            Assert.Equal("trimuser",      user.Username);
        }

        [Fact]
        public async Task Register_ShouldHashPassword_UsingPasswordService()
        {
            await _sut.Register("a@test.com", "auser", "mypassword", null, UserRole.Reader);

            _pwdMock.Verify(p => p.HashPassword("mypassword", null, out It.Ref<byte[]?>.IsAny), Times.Once);
        }

        [Fact]
        public async Task Register_ShouldCallCreateToken_WithCorrectArguments()
        {
            var (user, _) = await _sut.Register("b@test.com", "buser", "pass", null, UserRole.Blogger);

            _tokenMock.Verify(t => t.CreateToken(user.Id, "buser", "Blogger"), Times.Once);
        }

        [Fact]
        public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
        {
            SeedUser(email: "dup@test.com", username: "unique1");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Register("dup@test.com", "unique2", "pass", null, UserRole.Reader));
        }

        [Fact]
        public async Task Register_ShouldThrow_WhenUsernameAlreadyExists()
        {
            SeedUser(email: "unique@test.com", username: "dupuser");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Register("other@test.com", "dupuser", "pass", null, UserRole.Reader));
        }

        [Fact]
        public async Task Register_ShouldThrow_WhenBothEmailAndUsernameExist()
        {
            SeedUser(email: "both@test.com", username: "bothuser");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Register("both@test.com", "bothuser", "pass", null, UserRole.Reader));
        }

        [Fact]
        public async Task Register_ShouldStoreHashedPasswordAndKey()
        {
            var (user, _) = await _sut.Register("c@test.com", "cuser", "pass", null, UserRole.Reader);

            Assert.Equal(FakeHash, user.Password);
            Assert.Equal(FakeKey,  user.PasswordHash);
        }

        [Fact]
        public async Task Register_ShouldSetStatusToActive()
        {
            var (user, _) = await _sut.Register("d@test.com", "duser", "pass", null, UserRole.Reader);

            Assert.Equal("active", user.Status);
        }

        [Fact]
        public async Task Register_ShouldSetCreatedAtToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var (user, _) = await _sut.Register("e@test.com", "euser", "pass", null, UserRole.Reader);
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(user.CreatedAt, before, after);
        }

        [Fact]
        public async Task Register_ShouldAllowNullDisplayName()
        {
            var (user, _) = await _sut.Register("f@test.com", "fuser", "pass", null, UserRole.Reader);

            Assert.Null(user.DisplayName);
        }

        [Fact]
        public async Task Register_ShouldSupportAllRoles()
        {
            var (admin, _)   = await _sut.Register("admin@test.com",   "adminuser",   "pass", null, UserRole.Admin);
            var (blogger, _) = await _sut.Register("blogger@test.com", "bloggeruser", "pass", null, UserRole.Blogger);

            Assert.Equal(UserRole.Admin,   admin.Role);
            Assert.Equal(UserRole.Blogger, blogger.Role);
        }

        // ── Login ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_ShouldReturnUserAndToken_WhenCredentialsAreValid()
        {
            var user = SeedUser(email: "login@test.com", username: "loginuser");

            var (result, token) = await _sut.Login("login@test.com", "anypass");

            Assert.Equal(user.Id, result.Id);
            Assert.Equal(FakeToken, token);
        }

        [Fact]
        public async Task Login_ShouldAcceptUsername_AsIdentifier()
        {
            var user = SeedUser(email: "byname@test.com", username: "byname");

            var (result, _) = await _sut.Login("byname", "anypass");

            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task Login_ShouldTrimEmailOrUsername()
        {
            SeedUser(email: "trim@test.com", username: "trimlogin");

            var (result, _) = await _sut.Login("  trim@test.com  ", "anypass");

            Assert.Equal("trim@test.com", result.Email);
        }

        [Fact]
        public async Task Login_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.Login("ghost@test.com", "pass"));
        }

        [Fact]
        public async Task Login_ShouldThrow_WhenUserIsSuspended()
        {
            SeedUser(email: "sus@test.com", username: "sususer", suspended: true);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.Login("sus@test.com", "pass"));
        }

        [Fact]
        public async Task Login_ShouldThrow_WhenPasswordIsWrong()
        {
            SeedUser(email: "wrong@test.com", username: "wrongpass");

            // Return a DIFFERENT hash so FixedTimeEquals fails
            var wrongHash = new byte[] { 99, 99, 99, 99 };
            _pwdMock.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]?>(), out It.Ref<byte[]?>.IsAny))
                    .Callback(new HashPasswordCallback((string _, byte[]? _, out byte[]? key) => key = FakeKey))
                    .Returns(wrongHash);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.Login("wrong@test.com", "badpass"));
        }

        [Fact]
        public async Task Login_ShouldCallCreateToken_WithCorrectArguments()
        {
            var user = SeedUser(email: "tok@test.com", username: "tokuser", role: UserRole.Blogger);

            await _sut.Login("tok@test.com", "pass");

            _tokenMock.Verify(t => t.CreateToken(user.Id, "tokuser", "Blogger"), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldHashPassword_WithStoredKey()
        {
            var user = SeedUser(email: "hk@test.com", username: "hkuser");

            await _sut.Login("hk@test.com", "mypass");

            _pwdMock.Verify(p => p.HashPassword("mypass", user.PasswordHash, out It.Ref<byte[]?>.IsAny), Times.Once);
        }

        // ── ForgotPassword ────────────────────────────────────────────────────

        [Fact]
        public async Task ForgotPassword_ShouldReturnResetToken_WhenEmailExists()
        {
            SeedUser(email: "forgot@test.com", username: "forgotuser");
            _tokenMock.Setup(t => t.GeneratePasswordResetToken(It.IsAny<Guid>(), It.IsAny<TimeSpan>()))
                      .Returns(FakeResetToken);

            var result = await _sut.ForgotPassword("forgot@test.com");

            Assert.Equal(FakeResetToken, result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnEmpty_WhenEmailNotFound()
        {
            var result = await _sut.ForgotPassword("nobody@test.com");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnEmpty_WhenEmailIsWhitespace()
        {
            var result = await _sut.ForgotPassword("   ");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnEmpty_WhenEmailIsEmpty()
        {
            var result = await _sut.ForgotPassword(string.Empty);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldTrimEmail_BeforeLookup()
        {
            SeedUser(email: "spaced@test.com", username: "spaceduser");
            _tokenMock.Setup(t => t.GeneratePasswordResetToken(It.IsAny<Guid>(), It.IsAny<TimeSpan>()))
                      .Returns(FakeResetToken);

            var result = await _sut.ForgotPassword("  spaced@test.com  ");

            Assert.Equal(FakeResetToken, result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldGenerateTokenWith15MinuteTtl()
        {
            var user = SeedUser(email: "ttl@test.com", username: "ttluser");
            _tokenMock.Setup(t => t.GeneratePasswordResetToken(It.IsAny<Guid>(), It.IsAny<TimeSpan>()))
                      .Returns(FakeResetToken);

            await _sut.ForgotPassword("ttl@test.com");

            _tokenMock.Verify(t => t.GeneratePasswordResetToken(user.Id, TimeSpan.FromMinutes(15)), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ShouldNotRevealExistence_WhenEmailMissing()
        {
            // Returns empty — does NOT throw — so caller can't distinguish missing vs found
            var result = await _sut.ForgotPassword("nonexistent@test.com");

            Assert.Equal(string.Empty, result);
            _tokenMock.Verify(t => t.GeneratePasswordResetToken(It.IsAny<Guid>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        // ── ResetPassword ─────────────────────────────────────────────────────

        [Fact]
        public async Task ResetPassword_ShouldUpdatePasswordHash_WhenTokenIsValid()
        {
            var user = SeedUser(email: "reset@test.com", username: "resetuser");
            var newHash = new byte[] { 10, 20, 30, 40 };
            var newKey  = new byte[] { 50, 60, 70, 80 };

            _tokenMock.Setup(t => t.ValidatePasswordResetToken(FakeResetToken)).Returns(user.Id);
            _pwdMock.Setup(p => p.HashPassword("newpass123", null, out It.Ref<byte[]?>.IsAny))
                    .Callback(new HashPasswordCallback((string _, byte[]? _, out byte[]? key) => key = newKey))
                    .Returns(newHash);

            await _sut.ResetPassword(FakeResetToken, "newpass123");

            var updated = await _db.Users.FindAsync(user.Id);
            Assert.Equal(newHash, updated!.Password);
            Assert.Equal(newKey,  updated.PasswordHash);
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenTokenIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword(string.Empty, "newpass"));
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenTokenIsWhitespace()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword("   ", "newpass"));
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenNewPasswordIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword(FakeResetToken, string.Empty));
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenNewPasswordIsWhitespace()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword(FakeResetToken, "   "));
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenTokenValidationFails()
        {
            _tokenMock.Setup(t => t.ValidatePasswordResetToken(It.IsAny<string>()))
                      .Throws(new InvalidOperationException("Token expired"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword("bad.token", "newpass"));
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenUserNotFound()
        {
            var missingId = Guid.NewGuid();
            _tokenMock.Setup(t => t.ValidatePasswordResetToken(FakeResetToken)).Returns(missingId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword(FakeResetToken, "newpass"));
        }

        [Fact]
        public async Task ResetPassword_ShouldHashWithNewSalt_NotExistingKey()
        {
            var user = SeedUser(email: "salt@test.com", username: "saltuser");
            _tokenMock.Setup(t => t.ValidatePasswordResetToken(FakeResetToken)).Returns(user.Id);

            await _sut.ResetPassword(FakeResetToken, "newpass");

            // Must pass null as existingHashKey so a new salt is generated
            _pwdMock.Verify(p => p.HashPassword("newpass", null, out It.Ref<byte[]?>.IsAny), Times.Once);
        }
    }
}
