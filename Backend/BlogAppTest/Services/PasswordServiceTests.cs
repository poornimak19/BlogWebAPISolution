using BlogWebAPIApp.Services;

namespace BlogAppTest.Services
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _sut = new();

        [Fact]
        public void HashPassword_ShouldReturnHashAndKey_WhenNoExistingKey()
        {
            // Act
            var hash = _sut.HashPassword("mypassword", null, out var key);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotNull(key);
            Assert.NotEmpty(key);
        }

        [Fact]
        public void HashPassword_ShouldReturnSameHash_WhenSameKeyProvided()
        {
            // Arrange
            var first = _sut.HashPassword("mypassword", null, out var key);

            // Act
            var second = _sut.HashPassword("mypassword", key, out var sameKey);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(key, sameKey);
        }

        [Fact]
        public void HashPassword_ShouldReturnDifferentHash_WhenDifferentPassword()
        {
            // Arrange
            var hash1 = _sut.HashPassword("password1", null, out var key);

            // Act
            var hash2 = _sut.HashPassword("password2", key, out _);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashPassword_ShouldGenerateNewKey_WhenExistingKeyIsEmpty()
        {
            // Act
            var hash = _sut.HashPassword("pass", Array.Empty<byte>(), out var key);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(key!);
        }

        [Fact]
        public void HashPassword_ShouldProduceDifferentHashes_ForSamePasswordWithDifferentKeys()
        {
            // Arrange
            var hash1 = _sut.HashPassword("same", null, out var key1);
            var hash2 = _sut.HashPassword("same", null, out var key2);

            // Assert — different salts → different hashes
            Assert.NotEqual(key1, key2);
            Assert.NotEqual(hash1, hash2);
        }
    }
}
