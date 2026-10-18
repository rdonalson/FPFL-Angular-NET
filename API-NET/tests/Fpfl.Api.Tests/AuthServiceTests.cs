using Fpfl.Auth;
using Fpfl.Auth.Application.Services;
using Fpfl.Auth.Domain.Entities;
using Fpfl.Auth.Domain.Repositories;
using Fpfl.Auth.Infrastructure.Security;
using Fpfl.Common.Exceptions;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Fpfl.Api.Tests;

public sealed class AuthServiceTests
{
    private const string GuestEmail = "guest.user@gmail.com";

    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
    private readonly IOptions<AuthOptions> _options = Options.Create(new AuthOptions
    {
        SigningKey = "test-signing-key-that-is-at-least-32-characters-long",
        GuestEmail = GuestEmail
    });

    private readonly BCryptPasswordHasher _hasher = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _tokens;
    private readonly JwtTokenService _tokenService;
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _tokens = new FakeRefreshTokenRepository(_users);
        _tokenService = new JwtTokenService(_options, _clock);
        _auth = new AuthService(_users, _tokens, _hasher, _tokenService, _options, _clock);

        _users.Add(new User { Id = 1, Email = "rick@example.com", PasswordHash = _hasher.Hash("Correct#Pass1"), UserId = Guid.NewGuid(), First = "Rick", Last = "Donalson" }, "ROLE_ADMIN", "ROLE_USER");
        _users.Add(new User { Id = 2, Email = GuestEmail, PasswordHash = _hasher.Hash("Guest#Pass1"), UserId = Guid.NewGuid(), First = "Guest", Last = "User" }, "ROLE_USER");
    }

    [Fact]
    public async Task Login_with_correct_password_returns_session_and_stores_only_a_hash()
    {
        var session = await _auth.LoginAsync("Rick@Example.com", "Correct#Pass1", default);

        Assert.Equal(1, session.User.Id);
        Assert.False(session.IsGuest);
        Assert.Equal(new[] { "ROLE_ADMIN", "ROLE_USER" }, session.Roles);
        Assert.Equal(_clock.GetUtcNow().AddMinutes(15), session.AccessTokenExpiresAt);

        var stored = Assert.Single(_tokens.Tokens);
        Assert.NotEqual(session.RefreshToken, stored.Token);
        Assert.Equal(_tokenService.HashRefreshToken(session.RefreshToken), stored.Token);
    }

    [Theory]
    [InlineData("rick@example.com", "wrong-password")]
    [InlineData("nobody@example.com", "Correct#Pass1")]
    public async Task Login_failures_share_one_message(string email, string password)
    {
        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.LoginAsync(email, password, default));
        Assert.Equal("Invalid email or password", error.Message);
    }

    [Fact]
    public async Task Guest_login_needs_no_password_and_is_flagged()
    {
        var session = await _auth.GuestLoginAsync(default);

        Assert.Equal(GuestEmail, session.User.Email);
        Assert.True(session.IsGuest);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_the_old_one_stops_working()
    {
        var first = await _auth.LoginAsync("rick@example.com", "Correct#Pass1", default);

        var second = await _auth.RefreshAsync(first.RefreshToken, default);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.RefreshAsync(first.RefreshToken, default));
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected_and_deleted()
    {
        var session = await _auth.LoginAsync("rick@example.com", "Correct#Pass1", default);
        _clock.Advance(TimeSpan.FromDays(8));

        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.RefreshAsync(session.RefreshToken, default));

        Assert.Equal("Refresh token expired", error.Message);
        Assert.Empty(_tokens.Tokens);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var session = await _auth.LoginAsync("rick@example.com", "Correct#Pass1", default);

        await _auth.LogoutAsync(session.RefreshToken, default);

        Assert.Empty(_tokens.Tokens);
        await _auth.LogoutAsync("unknown-token", default); // no exception for unknown tokens
    }

    [Fact]
    public async Task Guest_cannot_change_password()
    {
        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => _auth.ChangePasswordAsync(2, "Guest#Pass1", "Another#Pass1", default));
    }

    [Fact]
    public async Task Wrong_current_password_is_a_validation_error_not_a_401()
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _auth.ChangePasswordAsync(1, "wrong-password", "Another#Pass1", default));
    }

    [Fact]
    public async Task Change_password_signs_out_other_devices()
    {
        var otherDevice = await _auth.LoginAsync("rick@example.com", "Correct#Pass1", default);

        var session = await _auth.ChangePasswordAsync(1, "Correct#Pass1", "Another#Pass1", default);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _auth.RefreshAsync(otherDevice.RefreshToken, default));
        await _auth.LoginAsync("rick@example.com", "Another#Pass1", default);
        Assert.False(string.IsNullOrEmpty(session.AccessToken));
    }

    [Fact]
    public async Task Register_rejects_an_existing_email()
    {
        await Assert.ThrowsAsync<DuplicateItemException>(
            () => _auth.RegisterAsync("RICK@example.com", "Some#Pass1", "R", "D", default));
    }

    [Fact]
    public async Task Register_creates_a_user_with_ROLE_USER()
    {
        var session = await _auth.RegisterAsync(" new@example.com ", "Some#Pass1", " New ", " Person ", default);

        Assert.Equal("new@example.com", session.User.Email);
        Assert.Equal("New", session.User.First);
        Assert.Equal(new[] { "ROLE_USER" }, session.Roles);
        Assert.True(_hasher.Verify("Some#Pass1", session.User.PasswordHash));
    }

    [Fact]
    public async Task Access_token_carries_the_expected_claims_and_validates()
    {
        var session = await _auth.GuestLoginAsync(default);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(session.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = "fpfl-api-net",
            ValidAudience = "fpfl-ui-ng",
            IssuerSigningKey = _options.Value.CreateSigningKey(),
            ValidateLifetime = false // the fixed test clock is in the past
        });

        Assert.True(result.IsValid, result.Exception?.Message);
        var token = (JsonWebToken)result.SecurityToken;
        Assert.Equal(GuestEmail, token.Subject);
        Assert.Equal("2", token.GetClaim(AuthClaims.Id).Value);
        Assert.Equal("true", token.GetClaim(AuthClaims.Guest).Value);
    }

    [Fact]
    public void Spring_style_2a_hashes_verify()
    {
        // Spring's BCryptPasswordEncoder writes $2a$10$...; the algorithm is identical for $2a/$2b.
        var hash = _hasher.Hash("Password@2");
        var springStyle = "$2a$" + hash[4..];

        Assert.Contains("$10$", hash);
        Assert.True(_hasher.Verify("Password@2", springStyle));
        Assert.False(_hasher.Verify("wrong", springStyle));
        Assert.False(_hasher.Verify("Password@2", "not-a-bcrypt-hash"));
    }

    // ------------------------------------------------------------------ fakes

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];
        private readonly Dictionary<long, List<string>> _roles = [];

        public void Add(User user, params string[] roles)
        {
            _users.Add(user);
            _roles[user.Id] = [.. roles];
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<string>> GetRoleNamesAsync(long userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(_roles.TryGetValue(userId, out var roles) ? roles.Order().ToList() : []);

        public Task<User> AddWithRoleAsync(User user, string roleName, CancellationToken cancellationToken)
        {
            user.Id = _users.Max(u => u.Id) + 1;
            Add(user, roleName);
            return Task.FromResult(user);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRefreshTokenRepository(FakeUserRepository users) : IRefreshTokenRepository
    {
        public List<RefreshToken> Tokens { get; } = [];

        public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(Tokens.FirstOrDefault(t => t.Token == tokenHash));

        public void Add(RefreshToken token)
        {
            // Mimic EF's Include(t => t.User) so RefreshAsync can read stored.User.
            token.User = users.FindByIdAsync(token.UserId, default).Result!;
            Tokens.Add(token);
        }

        public void Remove(RefreshToken token) => Tokens.Remove(token);

        public Task RemoveAllForUserAsync(long userId, CancellationToken cancellationToken)
        {
            Tokens.RemoveAll(t => t.UserId == userId);
            return Task.CompletedTask;
        }

        public Task RemoveExpiredForUserAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken)
        {
            Tokens.RemoveAll(t => t.UserId == userId && t.ExpiryDate <= nowUtc);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
