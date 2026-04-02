using BlogAppTest.Helpers;
using BlogWebAPIApp.Context;
using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using BlogWebAPIApp.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using static BlogWebAPIApp.Models.Enum;

namespace BlogAppTest.Services
{
    public class AuditLogServiceTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, AuditLog> _logs;
        private readonly InMemoryRepository<Guid, User>     _users;
        private readonly Mock<IHttpContextAccessor>          _httpMock;
        private readonly AuditLogService                     _sut;

        public AuditLogServiceTests()
        {
            _db       = TestDbContextFactory.Create();
            _logs     = new InMemoryRepository<Guid, AuditLog>(_db);
            _users    = new InMemoryRepository<Guid, User>(_db);
            _httpMock = new Mock<IHttpContextAccessor>();
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            _sut = new AuditLogService(_logs, _users, _httpMock.Object);
        }

        public void Dispose() => _db.Dispose();

        // ── Helpers ───────────────────────────────────────────────────────────

        private User SeedUser(UserRole role = UserRole.Reader)
        {
            var user = new User
            {
                Id           = Guid.NewGuid(),
                Username     = Guid.NewGuid().ToString("N")[..8],
                Email        = $"{Guid.NewGuid():N}@test.com",
                Password     = [],
                PasswordHash = [],
                Role         = role
            };
            _users.Seed([user]);
            return user;
        }

        private AuditLog SeedLog(
            string    action    = "Create",
            string    entity    = "Post",
            string    status    = "Success",
            Guid?     userId    = null,
            DateTime? timestamp = null)
        {
            var log = new AuditLog
            {
                Id         = Guid.NewGuid(),
                Action     = action,
                EntityName = entity,
                EntityId   = Guid.NewGuid().ToString(),
                UserId     = userId,
                Status     = status,
                Timestamp  = timestamp ?? DateTime.UtcNow
            };
            _logs.Seed([log]);
            return log;
        }

        private void SetupHttpContext(Guid? userId = null)
        {
            var ctx = new DefaultHttpContext();
            if (userId.HasValue)
            {
                var identity = new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())]);
                ctx.User = new ClaimsPrincipal(identity);
            }
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);
        }

        // ── LogAsync — basic persistence ──────────────────────────────────────

        [Fact]
        public async Task LogAsync_ShouldPersistLog_WithAllProvidedFields()
        {
            await _sut.LogAsync("Login", "User", "abc123", description: "User logged in");

            var log = _db.AuditLogs.Single();
            Assert.Equal("Login",          log.Action);
            Assert.Equal("User",           log.EntityName);
            Assert.Equal("abc123",         log.EntityId);
            Assert.Equal("User logged in", log.Description);
            Assert.Equal(AuditStatus.Success, log.Status);
        }

        [Fact]
        public async Task LogAsync_ShouldDefaultStatusToSuccess_WhenNotProvided()
        {
            await _sut.LogAsync("Delete", "Post", "id1");

            Assert.Equal(AuditStatus.Success, _db.AuditLogs.Single().Status);
        }

        [Fact]
        public async Task LogAsync_ShouldUseProvidedStatus_WhenSpecified()
        {
            await _sut.LogAsync("Login", "User", "id1", status: AuditStatus.Failed);

            Assert.Equal(AuditStatus.Failed, _db.AuditLogs.Single().Status);
        }

        [Fact]
        public async Task LogAsync_ShouldPersistOldAndNewValues()
        {
            await _sut.LogAsync("Update", "Post", "id1",
                oldValues: "{\"title\":\"old\"}", newValues: "{\"title\":\"new\"}");

            var log = _db.AuditLogs.Single();
            Assert.Equal("{\"title\":\"old\"}", log.OldValues);
            Assert.Equal("{\"title\":\"new\"}", log.NewValues);
        }

        [Fact]
        public async Task LogAsync_ShouldSetTimestampToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            await _sut.LogAsync("Create", "Post", "id1");
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(_db.AuditLogs.Single().Timestamp, before, after);
        }

        // ── LogAsync — userId resolution ──────────────────────────────────────

        [Fact]
        public async Task LogAsync_ShouldUseExplicitUserId_WhenProvided()
        {
            var userId = Guid.NewGuid();
            await _sut.LogAsync("Create", "Post", "id1", userId: userId);

            Assert.Equal(userId, _db.AuditLogs.Single().UserId);
        }

        [Fact]
        public async Task LogAsync_ShouldResolveUserIdFromHttpContext_WhenNotExplicit()
        {
            var userId = Guid.NewGuid();
            SetupHttpContext(userId);

            await _sut.LogAsync("Update", "Post", "id1");

            Assert.Equal(userId, _db.AuditLogs.Single().UserId);
        }

        [Fact]
        public async Task LogAsync_ShouldSetNullUserId_WhenNoContextAndNoExplicitId()
        {
            await _sut.LogAsync("Create", "Comment", "id1");

            Assert.Null(_db.AuditLogs.Single().UserId);
        }

        [Fact]
        public async Task LogAsync_ShouldPreferExplicitUserId_OverHttpContext()
        {
            var contextUserId  = Guid.NewGuid();
            var explicitUserId = Guid.NewGuid();
            SetupHttpContext(contextUserId);

            await _sut.LogAsync("Create", "Post", "id1", userId: explicitUserId);

            Assert.Equal(explicitUserId, _db.AuditLogs.Single().UserId);
        }

        // ── GetLogsAsync — basic retrieval ────────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldReturnAllLogs_WhenNoFilters()
        {
            SeedLog(); SeedLog(); SeedLog();

            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Equal(3, total);
            Assert.Equal(3, items.Count);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldReturnEmpty_WhenNoLogsExist()
        {
            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Equal(0, total);
            Assert.Empty(items);
        }

        // ── GetLogsAsync — ordering ───────────────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldReturnLogsOrderedByTimestampDescending()
        {
            var now = DateTime.UtcNow;
            SeedLog(timestamp: now.AddMinutes(-10));
            SeedLog(timestamp: now.AddMinutes(-5));
            SeedLog(timestamp: now);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.True(items[0].Timestamp >= items[1].Timestamp);
            Assert.True(items[1].Timestamp >= items[2].Timestamp);
        }

        // ── GetLogsAsync — filter by Action ──────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByAction()
        {
            SeedLog(action: "Create");
            SeedLog(action: "Delete");
            SeedLog(action: "Create");

            var (items, total) = await _sut.GetLogsAsync(
                new AuditLogFilterDto { Action = "Create", Page = 1, PageSize = 20 });

            Assert.Equal(2, total);
            Assert.All(items, i => Assert.Equal("Create", i.Action));
        }

        // ── GetLogsAsync — filter by EntityName ──────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByEntityName()
        {
            SeedLog(entity: "Post");
            SeedLog(entity: "Comment");
            SeedLog(entity: "Post");

            var (items, total) = await _sut.GetLogsAsync(
                new AuditLogFilterDto { EntityName = "Post", Page = 1, PageSize = 20 });

            Assert.Equal(2, total);
            Assert.All(items, i => Assert.Equal("Post", i.EntityName));
        }

        // ── GetLogsAsync — filter by Status ──────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByStatus()
        {
            SeedLog(status: AuditStatus.Success);
            SeedLog(status: AuditStatus.Failed);
            SeedLog(status: AuditStatus.Success);

            var (items, total) = await _sut.GetLogsAsync(
                new AuditLogFilterDto { Status = AuditStatus.Failed, Page = 1, PageSize = 20 });

            Assert.Equal(1, total);
            Assert.Equal(AuditStatus.Failed, items[0].Status);
        }

        // ── GetLogsAsync — filter by UserId ──────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByUserId()
        {
            var userId = Guid.NewGuid();
            SeedLog(userId: userId);
            SeedLog(userId: Guid.NewGuid());
            SeedLog(userId: userId);

            var (items, total) = await _sut.GetLogsAsync(
                new AuditLogFilterDto { UserId = userId, Page = 1, PageSize = 20 });

            Assert.Equal(2, total);
        }

        // ── GetLogsAsync — filter by date range ──────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByFromDate()
        {
            var now = DateTime.UtcNow;
            SeedLog(timestamp: now.AddDays(-5));
            SeedLog(timestamp: now.AddDays(-1));
            SeedLog(timestamp: now);

            var (_, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
            {
                From = now.AddDays(-2), Page = 1, PageSize = 20
            });

            Assert.Equal(2, total);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByToDate()
        {
            var now = DateTime.UtcNow;
            SeedLog(timestamp: now.AddDays(-5));
            SeedLog(timestamp: now.AddDays(-1));
            SeedLog(timestamp: now);

            var (_, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
            {
                To = now.AddDays(-2), Page = 1, PageSize = 20
            });

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldFilterByDateRange()
        {
            var now = DateTime.UtcNow;
            SeedLog(timestamp: now.AddDays(-10));
            SeedLog(timestamp: now.AddDays(-3));
            SeedLog(timestamp: now);

            var (_, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
            {
                From = now.AddDays(-5), To = now.AddDays(-1), Page = 1, PageSize = 20
            });

            Assert.Equal(1, total);
        }

        // ── GetLogsAsync — pagination ─────────────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldPaginateCorrectly()
        {
            for (int i = 0; i < 15; i++) SeedLog();

            var (page1, total) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 10 });
            var (page2, _)     = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 2, PageSize = 10 });

            Assert.Equal(15, total);
            Assert.Equal(10, page1.Count);
            Assert.Equal(5,  page2.Count);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldReturnEmpty_WhenPageExceedsTotal()
        {
            SeedLog(); SeedLog();

            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 5, PageSize = 10 });

            Assert.Equal(2, total);
            Assert.Empty(items);
        }

        // ── GetLogsAsync — user enrichment ────────────────────────────────────

        [Fact]
        public async Task GetLogsAsync_ShouldEnrichWithUsernameAndRole_WhenUserExists()
        {
            var user = SeedUser(UserRole.Blogger);
            SeedLog(userId: user.Id);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Equal(user.Username, items[0].Username);
            Assert.Equal("Blogger",    items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldEnrichAdminRole_Correctly()
        {
            var admin = SeedUser(UserRole.Admin);
            SeedLog(userId: admin.Id);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Equal("Admin", items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldEnrichReaderRole_Correctly()
        {
            var reader = SeedUser(UserRole.Reader);
            SeedLog(userId: reader.Id);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Equal("Reader", items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldSetNullUsernameAndRole_WhenUserIdIsNull()
        {
            SeedLog(userId: null);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Null(items[0].Username);
            Assert.Null(items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldSetNullUsernameAndRole_WhenUserNotInDb()
        {
            SeedLog(userId: Guid.NewGuid()); // userId exists in log but not in Users table

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Null(items[0].Username);
            Assert.Null(items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldEnrichMultipleDistinctUsers_InOnePage()
        {
            var admin   = SeedUser(UserRole.Admin);
            var blogger = SeedUser(UserRole.Blogger);
            SeedLog(userId: admin.Id);
            SeedLog(userId: blogger.Id);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            var adminLog   = items.First(i => i.Username == admin.Username);
            var bloggerLog = items.First(i => i.Username == blogger.Username);
            Assert.Equal("Admin",   adminLog.UserRole);
            Assert.Equal("Blogger", bloggerLog.UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldHandleMixedNullAndValidUserIds()
        {
            var user = SeedUser(UserRole.Reader);
            SeedLog(userId: user.Id);
            SeedLog(userId: null);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Equal(2, items.Count);
            var enriched = items.First(i => i.Username != null);
            var anonymous = items.First(i => i.Username == null);
            Assert.Equal(user.Username, enriched.Username);
            Assert.Null(anonymous.Username);
        }
    }

    // ── Additional branch coverage ────────────────────────────────────────────

    public class AuditLogServiceBranchTests : IDisposable
    {
        private readonly BlogContext _db;
        private readonly InMemoryRepository<Guid, AuditLog> _logs;
        private readonly InMemoryRepository<Guid, User>     _users;
        private readonly Mock<IHttpContextAccessor>          _httpMock;
        private readonly AuditLogService                     _sut;

        public AuditLogServiceBranchTests()
        {
            _db       = TestDbContextFactory.Create();
            _logs     = new InMemoryRepository<Guid, AuditLog>(_db);
            _users    = new InMemoryRepository<Guid, User>(_db);
            _httpMock = new Mock<IHttpContextAccessor>();
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            _sut = new AuditLogService(_logs, _users, _httpMock.Object);
        }

        public void Dispose() => _db.Dispose();

        // ── ResolveIpAddress: X-Forwarded-For single IP ───────────────────────

        [Fact]
        public async Task LogAsync_ShouldCaptureIpFromXForwardedFor_WhenHeaderPresent()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers["X-Forwarded-For"] = "203.0.113.5";
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);

            await _sut.LogAsync("Login", "User", "id1");

            Assert.Equal("203.0.113.5", _db.AuditLogs.Single().IpAddress);
        }

        // ── ResolveIpAddress: X-Forwarded-For multiple IPs (take first) ───────

        [Fact]
        public async Task LogAsync_ShouldTakeFirstIp_WhenXForwardedForHasMultiple()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers["X-Forwarded-For"] = "10.0.0.1, 192.168.1.1, 203.0.113.5";
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);

            await _sut.LogAsync("Login", "User", "id1");

            Assert.Equal("10.0.0.1", _db.AuditLogs.Single().IpAddress);
        }

        // ── ResolveIpAddress: no X-Forwarded-For, use RemoteIpAddress ─────────

        [Fact]
        public async Task LogAsync_ShouldUseRemoteIpAddress_WhenNoXForwardedFor()
        {
            var ctx = new DefaultHttpContext();
            ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.0.1");
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);

            await _sut.LogAsync("Login", "User", "id1");

            Assert.Equal("192.168.0.1", _db.AuditLogs.Single().IpAddress);
        }

        // ── ResolveIpAddress: null context → null IpAddress ───────────────────

        [Fact]
        public async Task LogAsync_ShouldSetNullIpAddress_WhenContextIsNull()
        {
            // Default mock returns null HttpContext
            await _sut.LogAsync("Create", "Post", "id1");

            Assert.Null(_db.AuditLogs.Single().IpAddress);
        }

        // ── ResolveUserId: via "sub" claim fallback ───────────────────────────

        [Fact]
        public async Task LogAsync_ShouldResolveUserId_FromSubClaim_WhenNameIdentifierAbsent()
        {
            var userId = Guid.NewGuid();
            var ctx    = new DefaultHttpContext();
            // Use "sub" claim only — no ClaimTypes.NameIdentifier
            var identity = new ClaimsIdentity([new Claim("sub", userId.ToString())]);
            ctx.User = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);

            await _sut.LogAsync("Login", "User", "id1");

            Assert.Equal(userId, _db.AuditLogs.Single().UserId);
        }

        // ── ResolveUserId: non-GUID sub → null ───────────────────────────────

        [Fact]
        public async Task LogAsync_ShouldSetNullUserId_WhenSubClaimIsNotAGuid()
        {
            var ctx      = new DefaultHttpContext();
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")]);
            ctx.User     = new ClaimsPrincipal(identity);
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);

            await _sut.LogAsync("Login", "User", "id1");

            Assert.Null(_db.AuditLogs.Single().UserId);
        }

        // ── UserAgent captured from context ───────────────────────────────────

        [Fact]
        public async Task LogAsync_ShouldCaptureUserAgent_WhenContextPresent()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers["User-Agent"] = "Mozilla/5.0 TestBrowser";
            _httpMock.Setup(x => x.HttpContext).Returns(ctx);

            await _sut.LogAsync("Create", "Post", "id1");

            Assert.Equal("Mozilla/5.0 TestBrowser", _db.AuditLogs.Single().UserAgent);
        }

        // ── UserAgent null when context is null ───────────────────────────────

        [Fact]
        public async Task LogAsync_ShouldSetNullUserAgent_WhenContextIsNull()
        {
            await _sut.LogAsync("Create", "Post", "id1");

            Assert.Null(_db.AuditLogs.Single().UserAgent);
        }

        // ── GetLogsAsync: all filters null/empty → no filtering ───────────────

        [Fact]
        public async Task GetLogsAsync_ShouldApplyNoFilters_WhenAllFilterFieldsAreNullOrEmpty()
        {
            for (int i = 0; i < 3; i++)
            {
                _logs.Seed([new AuditLog { Id = Guid.NewGuid(), Action = "Create", EntityName = "Post", EntityId = "x", Status = "Success", Timestamp = DateTime.UtcNow }]);
            }

            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
            {
                UserId = null, Action = "", EntityName = "", Status = "", From = null, To = null,
                Page = 1, PageSize = 20
            });

            Assert.Equal(3, total);
        }
    }
}
