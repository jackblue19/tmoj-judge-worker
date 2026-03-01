using Application.UseCases.Auth;
using Ardalis.Specification;
using Asp.Versioning;
using Infrastructure.Configurations.Auth;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Principal;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;

using Domain.Entities;
using System.Security.Cryptography;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Auth;


[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtOptions _jwt;
    private readonly GoogleOptions _google;
    private readonly TmojDbContext _db;

    public AuthController(
        ITokenService tokenService ,
        IRefreshTokenService refreshTokenService ,
        IPasswordHasher passwordHasher ,
        IOptions<JwtOptions> jwt ,
        IOptions<GoogleOptions> google ,
        TmojDbContext db)
    {
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;
        _google = google.Value;
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { Message = "pong" });


    //
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] CreateAccountRequest req ,
        CancellationToken ct)
    {
        try
        {
            var email = req.Email.ToLowerInvariant();
            if ( await _db.Users.AnyAsync(u => u.Email == email , ct) )
            {
                return BadRequest(new { Message = "Email already exists" });
            }

            var user = new User
            {
                FirstName = req.FirstName ,
                LastName = req.LastName ,
                Email = email ,
                Password = _passwordHasher.Hash(req.Password) ,
                Username = email.Split('@')[0] + Random.Shared.Next(1000 , 9999).ToString() ,
                DisplayName = $"{req.FirstName} {req.LastName}" ,
                LanguagePreference = "vi" ,
                Status = true ,
                EmailVerified = false
            };

            var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student" , ct);
            if ( studentRole != null )
            {
                user.UserRoleUsers.Add(new UserRole
                {
                    RoleId = studentRole.RoleId
                });
            }

            var verification = new EmailVerification
            {
                User = user ,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) ,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _db.Users.Add(user);
            _db.EmailVerifications.Add(verification);
            await _db.SaveChangesAsync(ct);

            // TODO: Send email with verification.Token

            return Ok(new { Message = "Registration successful. Please check your email to verify your account." , Token = verification.Token });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred during registration. Please try again later." });
        }
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest req ,
        CancellationToken ct)
    {
        try
        {
            var email = req.Email.ToLowerInvariant();
            var verification = await _db.EmailVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.User.Email == email && v.Token == req.Token , ct);

            if ( verification == null || verification.ExpiresAt < DateTime.UtcNow )
            {
                return BadRequest(new { Message = "Invalid or expired verification token." });
            }

            verification.User.EmailVerified = true;
            _db.EmailVerifications.Remove(verification);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Email verified successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred during email verification. Please try again later." });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if ( string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr , out var userId) )
            {
                return Unauthorized(new { Message = "Unauthorized access." });
            }

            // Revoke all active sessions for this user (or just the current one if tracked)
            // For simplicity, we'll revoke the latest session or the one matching the refresh token if provided.
            // Here we just clear the sessions/tokens for this user.
            var sessions = await _db.UserSessions
                .Where(s => s.UserId == userId)
                .Include(s => s.RefreshTokens)
                .ToListAsync(ct);

            _db.UserSessions.RemoveRange(sessions);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Logged out successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred during logout. Please try again later." });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req , CancellationToken ct)
    {
        try
        {
            var email = req.Email.ToLowerInvariant();
            var user = await _db.Users
                .Include(u => u.UserRoleUsers).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            if ( user == null || string.IsNullOrEmpty(user.Password) || !_passwordHasher.Verify(req.Password , user.Password) )
            {
                return Unauthorized(new { Message = "Invalid email or password" });
            }

            if ( !user.EmailVerified )
            {
                return BadRequest(new { Message = "Please verify your email before logging in." });
            }

            if ( !user.Status )
            {
                return BadRequest(new { Message = "Your account has been locked." });
            }

            // Create Session
            var session = new UserSession
            {
                UserId = user.UserId ,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ,
                UserAgent = Request.Headers["User-Agent"]
            };

            var refreshTokenStr = _refreshTokenService.GenerateToken();
            var refreshTokenHash = _refreshTokenService.HashToken(refreshTokenStr);

            var refreshToken = new RefreshToken
            {
                TokenHash = refreshTokenHash ,
                ExpireAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
            };

            session.RefreshTokens.Add(refreshToken);
            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync(ct);

            var roles = user.UserRoleUsers.Select(ur => ur.Role.RoleCode).ToList();
            if ( !roles.Any() ) roles.Add("user"); // Default role

            var accessToken = _tokenService.CreateAccessToken(
                user.UserId.ToString() ,
                user.DisplayName ?? user.Username ,
                roles);

            var userDto = new UserDto(
                UserId: user.UserId ,
                Email: user.Email ,
                FirstName: user.FirstName ,
                LastName: user.LastName ,
                DisplayName: user.DisplayName ,
                Username: user.Username ,
                AvatarUrl: user.AvatarUrl ,
                Roles: roles
            );

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(
                AccessToken: accessToken ,
                RefreshToken: refreshTokenStr ,
                ExpiresIn: _jwt.AccessTokenMinutes * 60 ,
                User: userDto
            ) , "Login successful"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred during login. Please try again later." });
        }
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req , CancellationToken ct)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(req.TokenId , new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _google.ClientId }
            });

            var email = payload.Email.ToLowerInvariant();

            if ( _google.AllowedDomains.Any() && !_google.AllowedDomains.Any(d => email.EndsWith($"@{d}" , StringComparison.OrdinalIgnoreCase)) )
            {
                return BadRequest(new { Message = "Login with this email domain is not allowed." });
            }

            var user = await _db.Users
                .Include(u => u.UserProviders)
                .Include(u => u.UserRoleUsers).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            if ( user == null )
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student" , ct);

                user = new User
                    {
                        Email = email,
                        FirstName = payload.GivenName ?? "",
                        LastName = payload.FamilyName ?? "",
                        DisplayName = payload.Name,
                        AvatarUrl = payload.Picture,
                        Username = email.Split('@')[0] + Random.Shared.Next(1000, 9999).ToString(),
                        EmailVerified = payload.EmailVerified,
                        LanguagePreference = "vi",
                        Status = true,
                        UserProviders = new List<UserProvider>(),
                        UserRoleUsers = new List<UserRole>()
                    };

                var provider = await _db.Providers
                    .FirstOrDefaultAsync(p => p.ProviderCode == "google", ct);

                if (provider == null)
                {
                    provider = new Provider
                    {
                        ProviderCode = "google",
                        ProviderDisplayName = "Google",
                        Enabled = true
                    };

                    _db.Providers.Add(provider);
                }

                user.UserProviders.Add(new UserProvider
                {
                    ProviderId = provider.ProviderId,
                    ProviderSubject = payload.Subject,
                    ProviderEmail = email,
                });

                if ( studentRole != null )
                {
                    user.UserRoleUsers.Add(new UserRole
                    {
                        RoleId = studentRole.RoleId,
                    });
                }

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
            }

            // Create Session
            var session = new UserSession
            {
                UserId = user.UserId ,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ,
                UserAgent = Request.Headers["User-Agent"]
            };

            var refreshTokenStr = _refreshTokenService.GenerateToken();
            var refreshTokenHash = _refreshTokenService.HashToken(refreshTokenStr);

            var refreshToken = new RefreshToken
            {
                TokenHash = refreshTokenHash ,
                ExpireAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
            };

            session.RefreshTokens.Add(refreshToken);
            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync(ct);

            var roles = user.UserRoleUsers.Select(ur => ur.Role.RoleCode).ToList();
            if ( !roles.Any() ) roles.Add("user"); // Default role

            var accessToken = _tokenService.CreateAccessToken(
                user.UserId.ToString() ,
                user.DisplayName ?? user.Username ,
                roles);

            var userDto = new UserDto(
                UserId: user.UserId ,
                Email: user.Email ,
                FirstName: user.FirstName ,
                LastName: user.LastName ,
                DisplayName: user.DisplayName ,
                Username: user.Username ,
                AvatarUrl: user.AvatarUrl ,
                Roles: roles
            );

            return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(
                AccessToken: accessToken ,
                RefreshToken: refreshTokenStr ,
                ExpiresIn: _jwt.AccessTokenMinutes * 60 ,
                User: userDto
            ) , "Login with Google successful"));
        }
        catch ( InvalidJwtException )
        {
            return BadRequest(new { Message = "Invalid Google Token" });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "Internal Server Error during Google Login. Please try again later." });
        }
    }
}
