using BlogWebAPIApp.Context;
using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly Mock<IPasswordService> _passwords;
        private readonly Mock<ITokenService> _tokens;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            var opts = new DbContextOptionsBuilder<BlogContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new BlogContext(opts);
            _passwords = new Mock<IPasswordService>();
            _tokens = new Mock<ITokenService>();
            _sut = new AuthService(_db, _passwords.Object, _tokens.Object);
        }

        public void Dispose() => _db.Dispose();

        // ── Register ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_ShouldReturnUserAndToken_WhenValidInput()
        {
            // Arrange
            var hash = new byte[] { 1, 2, 3 };
            var key  = new byte[] { 4, 5, 6 };
            byte[]? outKey = key;
            _passwords.Setup(p => p.HashPassword("pass123", null, out outKey)).Returns(hash);
            _tokens.Setup(t => t.CreateToken(It.IsAny<Guid>(), "alice", "Blogger")).Returns("jwt-token");

            // Act
            var (user, token) = await _sut.Register("alice@test.com", "alice", "pass123", "Alice", UserRole.Blogger);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("alice@test.com", user.Email);
            Assert.Equal("alice", user.Username);
            Assert.Equal("jwt-token", token);
        }

        [Fact]
        public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
        {
            // Arrange
            _db.Users.Add(new User { Email = "dup@test.com", Username = "other", Password = [], PasswordHash = [] });
            await _db.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Register("dup@test.com", "newuser", "pass", null, UserRole.Reader));
        }

        [Fact]
        public async Task Register_ShouldThrow_WhenUsernameAlreadyExists()
        {
            // Arrange
            _db.Users.Add(new User { Email = "unique@test.com", Username = "taken", Password = [], PasswordHash = [] });
            await _db.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Register("new@test.com", "taken", "pass", null, UserRole.Reader));
        }

        // ── Login ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_ShouldReturnUserAndToken_WhenCredentialsValid()
        {
            // Arrange
            var hash = new byte[64];
            var key  = new byte[128];
            new Random(42).NextBytes(hash);
            new Random(42).NextBytes(key);

            _db.Users.Add(new User
            {
                Email = "bob@test.com", Username = "bob",
                Password = hash, PasswordHash = key, IsSuspended = false
            });
            await _db.SaveChangesAsync();

            _passwords.Setup(p => p.HashPassword("secret", key, out It.Ref<byte[]?>.IsAny)).Returns(hash);
            _tokens.Setup(t => t.CreateToken(It.IsAny<Guid>(), "bob", It.IsAny<string>())).Returns("tok");

            // Act
            var (user, token) = await _sut.Login("bob", "secret");

            // Assert
            Assert.Equal("bob", user.Username);
            Assert.Equal("tok", token);
        }

        [Fact]
        public async Task Login_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.Login("nobody", "pass"));
        }

        [Fact]
        public async Task Login_ShouldThrow_WhenUserIsSuspended()
        {
            // Arrange
            _db.Users.Add(new User
            {
                Email = "sus@test.com", Username = "sus",
                Password = [1], PasswordHash = [2], IsSuspended = true
            });
            await _db.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.Login("sus", "pass"));
        }

        [Fact]
        public async Task Login_ShouldThrow_WhenPasswordWrong()
        {
            // Arrange
            var storedHash = new byte[] { 1, 2, 3 };
            var key        = new byte[] { 4, 5, 6 };
            _db.Users.Add(new User
            {
                Email = "u@test.com", Username = "u",
                Password = storedHash, PasswordHash = key, IsSuspended = false
            });
            await _db.SaveChangesAsync();

            var wrongHash = new byte[] { 9, 9, 9 };
            _passwords.Setup(p => p.HashPassword("wrong", key, out It.Ref<byte[]?>.IsAny)).Returns(wrongHash);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.Login("u", "wrong"));
        }

        // ── ForgotPassword ────────────────────────────────────────────────────

        [Fact]
        public async Task ForgotPassword_ShouldReturnToken_WhenEmailExists()
        {
            // Arrange
            _db.Users.Add(new User { Email = "fp@test.com", Username = "fp", Password = [], PasswordHash = [] });
            await _db.SaveChangesAsync();
            _tokens.Setup(t => t.GeneratePasswordResetToken(It.IsAny<Guid>(), It.IsAny<TimeSpan>())).Returns("reset-tok");

            // Act
            var result = await _sut.ForgotPassword("fp@test.com");

            // Assert
            Assert.Equal("reset-tok", result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnEmpty_WhenEmailNotFound()
        {
            var result = await _sut.ForgotPassword("ghost@test.com");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ForgotPassword_ShouldReturnEmpty_WhenEmailIsWhitespace()
        {
            var result = await _sut.ForgotPassword("   ");
            Assert.Equal(string.Empty, result);
        }

        // ── ResetPassword ─────────────────────────────────────────────────────

        [Fact]
        public async Task ResetPassword_ShouldUpdatePassword_WhenTokenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _db.Users.Add(new User { Id = userId, Email = "r@test.com", Username = "r", Password = [1], PasswordHash = [2] });
            await _db.SaveChangesAsync();

            _tokens.Setup(t => t.ValidatePasswordResetToken("valid-tok")).Returns(userId);
            var newHash = new byte[] { 10, 11 };
            var newKey  = new byte[] { 12, 13 };
            byte[]? outKey = newKey;
            _passwords.Setup(p => p.HashPassword("newpass", null, out outKey)).Returns(newHash);

            // Act
            await _sut.ResetPassword("valid-tok", "newpass");

            // Assert
            var user = await _db.Users.FindAsync(userId);
            Assert.Equal(newHash, user!.Password);
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenTokenIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword("", "newpass"));
        }

        [Fact]
        public async Task ResetPassword_ShouldThrow_WhenNewPasswordIsEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ResetPassword("tok", ""));
        }
    }
}
