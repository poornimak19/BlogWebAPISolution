using BlogWebAPIApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BlogAppTest.Services
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _sut;

        public JwtTokenServiceTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"]      = "super-secret-key-that-is-long-enough-for-hmac256",
                    ["Jwt:Issuer"]   = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience"
                })
                .Build();

            _sut = new JwtTokenService(config);
        }

        // ── CreateToken ───────────────────────────────────────────────────────

        [Fact]
        public void CreateToken_ShouldReturnNonEmptyString()
        {
            var token = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void CreateToken_ShouldReturnDifferentTokens_ForDifferentUsers()
        {
            var t1 = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");
            var t2 = _sut.CreateToken(Guid.NewGuid(), "bob",   "Reader");
            Assert.NotEqual(t1, t2);
        }

        // ── GeneratePasswordResetToken ────────────────────────────────────────

        [Fact]
        public void GeneratePasswordResetToken_ShouldReturnNonEmptyString()
        {
            var token = _sut.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        // ── ValidatePasswordResetToken ────────────────────────────────────────

        [Fact]
        public void ValidatePasswordResetToken_ShouldReturnUserId_WhenTokenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token  = _sut.GeneratePasswordResetToken(userId, TimeSpan.FromMinutes(15));

            // Act
            var result = _sut.ValidatePasswordResetToken(token);

            // Assert
            Assert.Equal(userId, result);
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTokenIsGarbage()
        {
            // ArgumentException is a subclass of Exception — use ThrowsAny
            Assert.ThrowsAny<Exception>(() => _sut.ValidatePasswordResetToken("not.a.jwt"));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTokenIsExpired()
        {
            // Build a second service with a different key so the token signature is invalid
            var otherConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"]      = "a-completely-different-secret-key-for-testing",
                    ["Jwt:Issuer"]   = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience"
                })
                .Build();
            var otherService = new JwtTokenService(otherConfig);
            var tokenFromOtherKey = otherService.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));

            // Validating a token signed with a different key should throw
            Assert.ThrowsAny<SecurityTokenException>(() =>
                _sut.ValidatePasswordResetToken(tokenFromOtherKey));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenRegularAuthTokenUsed()
        {
            // A regular auth token lacks the "typ=reset" claim
            var authToken = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");
            Assert.Throws<SecurityTokenException>(() =>
                _sut.ValidatePasswordResetToken(authToken));
        }
    }
}
