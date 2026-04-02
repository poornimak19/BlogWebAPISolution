using BlogWebAPIApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlogAppTest.Services
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _sut;
        private const string Key      = "super-secret-key-that-is-long-enough-for-hmac256";
        private const string Issuer   = "TestIssuer";
        private const string Audience = "TestAudience";

        public JwtTokenServiceTests()
        {
            _sut = Build(Key, Issuer, Audience);
        }

        private static JwtTokenService Build(string key, string issuer, string audience)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"]      = key,
                    ["Jwt:Issuer"]   = issuer,
                    ["Jwt:Audience"] = audience
                })
                .Build();
            return new JwtTokenService(config);
        }

        private static JwtSecurityToken Decode(string raw)
            => new JwtSecurityTokenHandler().ReadJwtToken(raw);

        // ── CreateToken — output shape ────────────────────────────────────────

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

        [Fact]
        public void CreateToken_ShouldContainSubClaim_EqualToUserId()
        {
            var userId = Guid.NewGuid();
            var raw    = _sut.CreateToken(userId, "alice", "Blogger");
            var jwt    = Decode(raw);

            var sub = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            Assert.Equal(userId.ToString(), sub);
        }

        [Fact]
        public void CreateToken_ShouldContainUsernameClaim()
        {
            var raw = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");
            var jwt = Decode(raw);

            Assert.Contains(jwt.Claims, c => c.Type == "username" && c.Value == "alice");
        }

        [Fact]
        public void CreateToken_ShouldContainRoleClaim()
        {
            var raw = _sut.CreateToken(Guid.NewGuid(), "alice", "Admin");
            var jwt = Decode(raw);

            Assert.Contains(jwt.Claims, c =>
                (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");
        }

        [Fact]
        public void CreateToken_ShouldContainNameIdentifierClaim()
        {
            var userId = Guid.NewGuid();
            var raw    = _sut.CreateToken(userId, "alice", "Reader");
            var jwt    = Decode(raw);

            Assert.Contains(jwt.Claims, c =>
                c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        }

        [Fact]
        public void CreateToken_ShouldExpireInApproximately2Hours()
        {
            var before = DateTime.UtcNow.AddHours(1).AddMinutes(55);
            var raw    = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");
            var after  = DateTime.UtcNow.AddHours(2).AddMinutes(5);
            var jwt    = Decode(raw);

            Assert.InRange(jwt.ValidTo, before, after);
        }

        [Fact]
        public void CreateToken_ShouldHaveCorrectIssuerAndAudience()
        {
            var raw = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");
            var jwt = Decode(raw);

            Assert.Equal(Issuer,   jwt.Issuer);
            Assert.Contains(Audience, jwt.Audiences);
        }

        [Fact]
        public void CreateToken_ShouldProduceDifferentTokens_ForSameUserDifferentRoles()
        {
            var userId = Guid.NewGuid();
            var t1 = _sut.CreateToken(userId, "alice", "Reader");
            var t2 = _sut.CreateToken(userId, "alice", "Admin");
            Assert.NotEqual(t1, t2);
        }

        // ── GeneratePasswordResetToken ────────────────────────────────────────

        [Fact]
        public void GeneratePasswordResetToken_ShouldReturnNonEmptyString()
        {
            var token = _sut.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GeneratePasswordResetToken_ShouldContainTypResetClaim()
        {
            var raw = _sut.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));
            var jwt = Decode(raw);

            Assert.Contains(jwt.Claims, c => c.Type == "typ" && c.Value == "reset");
        }

        [Fact]
        public void GeneratePasswordResetToken_ShouldContainSubClaim_EqualToUserId()
        {
            var userId = Guid.NewGuid();
            var raw    = _sut.GeneratePasswordResetToken(userId, TimeSpan.FromMinutes(15));
            var jwt    = Decode(raw);

            var sub = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            Assert.Equal(userId.ToString(), sub);
        }

        [Fact]
        public void GeneratePasswordResetToken_ShouldExpireAccordingToTtl()
        {
            var ttl    = TimeSpan.FromMinutes(30);
            var before = DateTime.UtcNow.Add(ttl).AddSeconds(-5);
            var raw    = _sut.GeneratePasswordResetToken(Guid.NewGuid(), ttl);
            var after  = DateTime.UtcNow.Add(ttl).AddSeconds(5);
            var jwt    = Decode(raw);

            Assert.InRange(jwt.ValidTo, before, after);
        }

        [Fact]
        public void GeneratePasswordResetToken_ShouldProduceDifferentTokens_ForDifferentUsers()
        {
            var t1 = _sut.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));
            var t2 = _sut.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));
            Assert.NotEqual(t1, t2);
        }

        // ── ValidatePasswordResetToken — happy path ───────────────────────────

        [Fact]
        public void ValidatePasswordResetToken_ShouldReturnUserId_WhenTokenValid()
        {
            var userId = Guid.NewGuid();
            var token  = _sut.GeneratePasswordResetToken(userId, TimeSpan.FromMinutes(15));

            var result = _sut.ValidatePasswordResetToken(token);

            Assert.Equal(userId, result);
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldWork_WithShortTtl()
        {
            var userId = Guid.NewGuid();
            var token  = _sut.GeneratePasswordResetToken(userId, TimeSpan.FromSeconds(60));

            var result = _sut.ValidatePasswordResetToken(token);

            Assert.Equal(userId, result);
        }

        // ── ValidatePasswordResetToken — failure branches ─────────────────────

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTokenIsGarbage()
        {
            Assert.ThrowsAny<Exception>(() => _sut.ValidatePasswordResetToken("not.a.jwt"));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTokenSignedWithDifferentKey()
        {
            var other = Build("a-completely-different-secret-key-for-testing-purposes", Issuer, Audience);
            var token = other.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));

            Assert.ThrowsAny<SecurityTokenException>(() => _sut.ValidatePasswordResetToken(token));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenRegularAuthTokenUsed()
        {
            // Auth token has no "typ=reset" claim → should throw SecurityTokenException
            var authToken = _sut.CreateToken(Guid.NewGuid(), "alice", "Blogger");

            Assert.Throws<SecurityTokenException>(() => _sut.ValidatePasswordResetToken(authToken));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenWrongIssuer()
        {
            var other = Build(Key, "WrongIssuer", Audience);
            var token = other.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));

            Assert.ThrowsAny<SecurityTokenException>(() => _sut.ValidatePasswordResetToken(token));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenWrongAudience()
        {
            var other = Build(Key, Issuer, "WrongAudience");
            var token = other.GeneratePasswordResetToken(Guid.NewGuid(), TimeSpan.FromMinutes(15));

            Assert.ThrowsAny<SecurityTokenException>(() => _sut.ValidatePasswordResetToken(token));
        }

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenEmptyString()
        {
            Assert.ThrowsAny<Exception>(() => _sut.ValidatePasswordResetToken(string.Empty));
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class JwtTokenServiceBranchTests
    {
        private const string Key      = "super-secret-key-that-is-long-enough-for-hmac256";
        private const string Issuer   = "TestIssuer";
        private const string Audience = "TestAudience";

        private static JwtTokenService Build(string key = Key, string issuer = Issuer, string audience = Audience)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = key, ["Jwt:Issuer"] = issuer, ["Jwt:Audience"] = audience
                }).Build();
            return new JwtTokenService(config);
        }

        // ── ValidatePasswordResetToken: sub claim missing, NameIdentifier used ─

        [Fact]
        public void ValidatePasswordResetToken_ShouldReturnUserId_WhenSubClaimMissing_ButNameIdentifierPresent()
        {
            // Build a token manually that has NameIdentifier but no "sub" claim
            var sut    = Build();
            var userId = Guid.NewGuid();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = Key, ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience
                }).Build();

            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim("typ", "reset")
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw    = new JwtSecurityTokenHandler().WriteToken(token);
            var result = sut.ValidatePasswordResetToken(raw);

            Assert.Equal(userId, result);
        }

        // ── ValidatePasswordResetToken: typ claim present but wrong value ──────

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTypClaimIsWrongValue()
        {
            var sut    = Build();
            var userId = Guid.NewGuid();

            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim("typ", "access") // wrong purpose
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw = new JwtSecurityTokenHandler().WriteToken(token);

            Assert.Throws<SecurityTokenException>(() => sut.ValidatePasswordResetToken(raw));
        }

        // ── ValidatePasswordResetToken: typ claim missing entirely ────────────

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTypClaimMissing()
        {
            var sut    = Build();
            var userId = Guid.NewGuid();

            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw = new JwtSecurityTokenHandler().WriteToken(token);

            Assert.Throws<SecurityTokenException>(() => sut.ValidatePasswordResetToken(raw));
        }

        // ── ValidatePasswordResetToken: sub is not a valid GUID ───────────────

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenSubIsNotAGuid()
        {
            var sut = Build();

            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"),
                    new Claim("typ", "reset")
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw = new JwtSecurityTokenHandler().WriteToken(token);

            Assert.Throws<SecurityTokenException>(() => sut.ValidatePasswordResetToken(raw));
        }

        // ── ValidatePasswordResetToken: sub whitespace → invalid subject ──────

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenSubIsWhitespace()
        {
            var sut = Build();
            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "   "),
                    new Claim("typ", "reset")
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw = new JwtSecurityTokenHandler().WriteToken(token);
            Assert.Throws<SecurityTokenException>(() => sut.ValidatePasswordResetToken(raw));
        }

        // ── ValidatePasswordResetToken: expired token ─────────────────────────

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenTokenIsExpired()
        {
            var sut   = Build();
            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            // Token that expired 10 minutes ago (beyond the 30s ClockSkew)
            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                    new Claim("typ", "reset")
                },
                notBefore: DateTime.UtcNow.AddMinutes(-20),
                expires: DateTime.UtcNow.AddMinutes(-10),
                signingCredentials: creds
            );

            var raw = new JwtSecurityTokenHandler().WriteToken(token);
            Assert.ThrowsAny<SecurityTokenException>(() => sut.ValidatePasswordResetToken(raw));
        }

        // ── ValidatePasswordResetToken: jwtToken null branch via wrong alg ────
        // Build a service that accepts HS256 tokens but validate a token whose
        // header claims a different alg — hits the !Alg.Equals branch.

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenHeaderAlgIsNotHmacSha256()
        {
            // Create a valid HS256 token, then manually replace the alg in the header
            // to "HS384" so it passes signature check but fails the alg assertion.
            var sut    = Build();
            var userId = Guid.NewGuid();

            // Step 1: generate a valid reset token
            var validRaw = sut.GeneratePasswordResetToken(userId, TimeSpan.FromMinutes(15));

            // Step 2: decode the three parts
            var parts  = validRaw.Split('.');
            var header = parts[0];

            // Step 3: decode header, replace alg, re-encode (no re-signing — will fail sig check)
            var headerJson = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(header.PadRight(header.Length + (4 - header.Length % 4) % 4, '=')));
            var tamperedHeader = headerJson.Replace("\"HS256\"", "\"HS384\"");
            var tamperedEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tamperedHeader))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            var tamperedToken = $"{tamperedEncoded}.{parts[1]}.{parts[2]}";

            // Tampered token will fail signature validation — still exercises the exception path
            Assert.ThrowsAny<SecurityTokenException>(() => sut.ValidatePasswordResetToken(tamperedToken));
        }

        // ── ValidatePasswordResetToken: both sub and NameIdentifier missing ───

        [Fact]
        public void ValidatePasswordResetToken_ShouldThrow_WhenBothSubAndNameIdentifierMissing()
        {
            var sut   = Build();
            var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            // Token with typ=reset but no sub or nameidentifier claim
            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[] { new Claim("typ", "reset") },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw = new JwtSecurityTokenHandler().WriteToken(token);
            Assert.Throws<SecurityTokenException>(() => sut.ValidatePasswordResetToken(raw));
        }

        // ── ValidatePasswordResetToken: NameIdentifier fallback with valid GUID

        [Fact]
        public void ValidatePasswordResetToken_ShouldUseNameIdentifier_WhenSubAbsent()
        {
            var sut    = Build();
            var userId = Guid.NewGuid();
            var key    = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Key));
            var creds  = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer, audience: Audience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim("typ", "reset")
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var raw    = new JwtSecurityTokenHandler().WriteToken(token);
            var result = sut.ValidatePasswordResetToken(raw);
            Assert.Equal(userId, result);
        }
    }
}
