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
        private readonly Mock<IHttpContextAccessor> _httpMock;
        private readonly AuditLogService _sut;

        public AuditLogServiceTests()
        {
            _db       = TestDbContextFactory.Create();
            _httpMock = new Mock<IHttpContextAccessor>();
            _httpMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            _sut = new AuditLogService(_db, _httpMock.Object);
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
            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        private AuditLog SeedLog(
            string action     = "Create",
            string entity     = "Post",
            string status     = "Success",
            Guid?  userId     = null,
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
            _db.AuditLogs.Add(log);
            _db.SaveChanges();
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

        // ── LogAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task LogAsync_ShouldPersistAuditLog_WithProvidedFields()
        {
            await _sut.LogAsync("Login", "User", "abc123", description: "User logged in");

            var log = _db.AuditLogs.Single();
            Assert.Equal("Login",        log.Action);
            Assert.Equal("User",         log.EntityName);
            Assert.Equal("abc123",       log.EntityId);
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
            // HttpContext is null (default mock setup)
            await _sut.LogAsync("Create", "Comment", "id1");

            Assert.Null(_db.AuditLogs.Single().UserId);
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

            var ts = _db.AuditLogs.Single().Timestamp;
            Assert.InRange(ts, before, after);
        }

        // ── GetLogsAsync — no filter ──────────────────────────────────────────

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

            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
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

            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
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

            var (items, total) = await _sut.GetLogsAsync(new AuditLogFilterDto
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
        public async Task GetLogsAsync_ShouldSetNullUsernameAndRole_WhenUserIdIsNull()
        {
            SeedLog(userId: null);

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Null(items[0].Username);
            Assert.Null(items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldSetNullUsernameAndRole_WhenUserIdNotInDb()
        {
            SeedLog(userId: Guid.NewGuid()); // user doesn't exist in Users table

            var (items, _) = await _sut.GetLogsAsync(new AuditLogFilterDto { Page = 1, PageSize = 20 });

            Assert.Null(items[0].Username);
            Assert.Null(items[0].UserRole);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldEnrichMultipleDistinctUsers()
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
    }
}
