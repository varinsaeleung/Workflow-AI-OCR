using FluentAssertions;
using KmOcr.Application.Auth;
using KmOcr.Application.Common;
using KmOcr.Application.Contracts.Persistence;
using KmOcr.Application.Contracts.Security;
using KmOcr.Domain.Audit;
using KmOcr.Domain.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KmOcr.Application.Tests;

/// <summary>
/// Verifies authentication use cases with in-memory fakes.
/// </summary>
public sealed class AuthModuleTests
{
    /// <summary>
    /// Ensures valid credentials issue both tokens, persist the refresh token, and write an audit event.
    /// </summary>
    [Fact]
    public async Task LoginAsync_should_issue_tokens_and_audit_success()
    {
        var hasher = new FakePasswordHasher();
        var user = User.Create("admin@km.local", "System Admin", hasher.HashPassword("Pass@123"));
        var users = new FakeUserRepository(user, ["documents.read", "dashboard.read"]);
        var refreshTokens = new FakeRefreshTokenRepository();
        var auditLogs = new RecordingAuditLogRepository();
        var unitOfWork = new FakeAuthUnitOfWork();
        var module = new AuthModule(
            users,
            refreshTokens,
            hasher,
            new FakeTokenService(["refresh-token"]),
            auditLogs,
            unitOfWork,
            NullLogger<AuthModule>.Instance);

        var result = await module.LoginAsync(new LoginCommand(" ADMIN@KM.LOCAL ", "Pass@123", "127.0.0.1", "unit-test"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("admin@km.local");
        result.Permissions.Should().BeEquivalentTo("documents.read", "dashboard.read");
        user.LastLoginAt.Should().NotBeNull();
        refreshTokens.AddedTokens.Should().ContainSingle(token => token.TokenHash == "hash:refresh-token");
        refreshTokens.AddedTokens.Single().ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        auditLogs.Actions.Should().Contain("auth.login");
        unitOfWork.SaveCount.Should().Be(1);
    }

    /// <summary>
    /// Ensures invalid credentials do not issue tokens and still leave an audit trace.
    /// </summary>
    [Fact]
    public async Task LoginAsync_should_reject_invalid_password_and_audit_failure()
    {
        var hasher = new FakePasswordHasher();
        var user = User.Create("admin@km.local", "System Admin", hasher.HashPassword("Pass@123"));
        var users = new FakeUserRepository(user, ["documents.read"]);
        var refreshTokens = new FakeRefreshTokenRepository();
        var auditLogs = new RecordingAuditLogRepository();
        var unitOfWork = new FakeAuthUnitOfWork();
        var module = new AuthModule(
            users,
            refreshTokens,
            hasher,
            new FakeTokenService(["refresh-token"]),
            auditLogs,
            unitOfWork,
            NullLogger<AuthModule>.Instance);

        var act = () => module.LoginAsync(new LoginCommand("admin@km.local", "wrong", "127.0.0.1", "unit-test"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid email or password.");
        refreshTokens.AddedTokens.Should().BeEmpty();
        auditLogs.Actions.Should().Contain("auth.login_failed");
        unitOfWork.SaveCount.Should().Be(1);
    }

    /// <summary>
    /// Ensures refresh token exchange revokes the old token and stores a replacement.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_should_rotate_refresh_token()
    {
        var hasher = new FakePasswordHasher();
        var user = User.Create("admin@km.local", "System Admin", hasher.HashPassword("Pass@123"));
        var existingToken = RefreshToken.Create(user.Id, "hash:old-refresh", DateTimeOffset.UtcNow.AddDays(1), "127.0.0.1", "unit-test");
        var users = new FakeUserRepository(user, ["documents.read"]);
        var refreshTokens = new FakeRefreshTokenRepository(existingToken);
        var auditLogs = new RecordingAuditLogRepository();
        var unitOfWork = new FakeAuthUnitOfWork();
        var module = new AuthModule(
            users,
            refreshTokens,
            hasher,
            new FakeTokenService(["new-refresh"]),
            auditLogs,
            unitOfWork,
            NullLogger<AuthModule>.Instance);

        var result = await module.RefreshAsync(new RefreshTokenCommand("old-refresh", "127.0.0.1", "unit-test"), CancellationToken.None);

        result.RefreshToken.Should().Be("new-refresh");
        existingToken.IsActive.Should().BeFalse();
        existingToken.ReplacedByTokenHash.Should().Be("hash:new-refresh");
        refreshTokens.AddedTokens.Should().ContainSingle(token => token.TokenHash == "hash:new-refresh");
        auditLogs.Actions.Should().Contain("auth.refresh");
        unitOfWork.SaveCount.Should().Be(1);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user;
        private readonly IReadOnlyList<string> _permissions;

        /// <summary>
        /// Creates the fake repository with one user and permission set.
        /// </summary>
        public FakeUserRepository(User user, IReadOnlyList<string> permissions)
        {
            _user = user;
            _permissions = permissions;
        }

        /// <summary>
        /// Stores a newly created user in memory.
        /// </summary>
        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the configured user when the normalized email matches.
        /// </summary>
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Equals(_user.Email, email.Trim().ToLowerInvariant(), StringComparison.Ordinal) ? _user : null);
        }

        /// <summary>
        /// Returns the configured user when the identifier matches.
        /// </summary>
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user.Id == id ? _user : null);
        }

        /// <summary>
        /// Returns the configured permission code list.
        /// </summary>
        public Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user.Id == userId ? _permissions : []);
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly RefreshToken? _existingToken;

        /// <summary>
        /// Creates the fake repository with an optional existing token.
        /// </summary>
        public FakeRefreshTokenRepository(RefreshToken? existingToken = null)
        {
            _existingToken = existingToken;
        }

        /// <summary>
        /// Gets tokens added during the test.
        /// </summary>
        public List<RefreshToken> AddedTokens { get; } = [];

        /// <summary>
        /// Stores the refresh token in memory.
        /// </summary>
        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            AddedTokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the configured refresh token when the hash matches.
        /// </summary>
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(_existingToken?.TokenHash == tokenHash ? _existingToken : null);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        /// <summary>
        /// Returns a deterministic hash for test readability.
        /// </summary>
        public string HashPassword(string password)
        {
            return $"hash:{password}";
        }

        /// <summary>
        /// Verifies deterministic hashes.
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            return passwordHash == HashPassword(password);
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        private readonly Queue<string> _refreshTokens;

        /// <summary>
        /// Creates the token service with queued refresh token values.
        /// </summary>
        public FakeTokenService(IEnumerable<string> refreshTokens)
        {
            _refreshTokens = new Queue<string>(refreshTokens);
        }

        /// <summary>
        /// Returns a deterministic access token payload.
        /// </summary>
        public AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> permissions)
        {
            return new AccessTokenResult("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
        }

        /// <summary>
        /// Returns the next queued refresh token.
        /// </summary>
        public string CreateRefreshToken()
        {
            return _refreshTokens.Dequeue();
        }

        /// <summary>
        /// Hashes refresh tokens deterministically for assertions.
        /// </summary>
        public string HashRefreshToken(string refreshToken)
        {
            return $"hash:{refreshToken}";
        }

        /// <summary>
        /// Returns a deterministic future refresh token expiry.
        /// </summary>
        public DateTimeOffset GetRefreshTokenExpiry()
        {
            return DateTimeOffset.UtcNow.AddDays(7);
        }
    }

    private sealed class RecordingAuditLogRepository : IAuditLogRepository
    {
        /// <summary>
        /// Gets the recorded audit actions.
        /// </summary>
        public List<string> Actions { get; } = [];

        /// <summary>
        /// Records the audit action in memory.
        /// </summary>
        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            Actions.Add(auditLog.Action);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuthUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Gets the number of save operations.
        /// </summary>
        public int SaveCount { get; private set; }

        /// <summary>
        /// Counts the save operation without a database.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
