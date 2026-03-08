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
using System.Security.Claims;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Serilog.Core;
using Microsoft.Extensions.Configuration;

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
    private readonly GithubOptions _github;
    private readonly TmojDbContext _db;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;

    public AuthController(
        ITokenService tokenService ,
        IRefreshTokenService refreshTokenService ,
        IPasswordHasher passwordHasher ,
        IOptions<JwtOptions> jwt ,
        IOptions<GoogleOptions> google ,
        IOptions<GithubOptions> github ,
        ILogger<AuthController> logger,
        TmojDbContext db,
        IConfiguration config)
    {
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;
        _google = google.Value;
        _github = github.Value;
        _db = db;
        _logger = logger;
        _config = config;
    }

    [AllowAnonymous]
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { Message = "pong" });


    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] CreateAccountRequest req ,
        CancellationToken ct)

    {
       _logger.LogInformation("Register endpoint called");
        try
        {
            var email = req.Email.ToLowerInvariant();
            if ( await _db.Users.AnyAsync(u => u.Email == email , ct) )
            {
                return BadRequest(new { Message = "Email already exists" });
            }
            bool IsFptEmail(string email)
                {
                    if (string.IsNullOrWhiteSpace(email))
                        return false;

                    return email
                        .ToLowerInvariant()
                        .EndsWith("@fpt.edu.vn");
                }
            var roleCode = IsFptEmail(email) ? "admin" : "student";

            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);

            if (role == null)
                throw new Exception("Role not found");
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
                EmailVerified = false,
        RoleId = role.RoleId
    };

            var selectedRole = await _db.Roles
     .FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);

            if (selectedRole == null)
                throw new Exception("Role not found");

            user.UserRoleUsers.Add(new UserRole
            {
                RoleId = selectedRole.RoleId
            });

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
                .Include(v => v.User).ThenInclude(u => u.UserRoleUsers).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(v => v.User.Email == email && v.Token == req.Token , ct);

            if ( verification == null || verification.ExpiresAt < DateTime.UtcNow )
            {
                return BadRequest(new { Message = "Invalid or expired verification token." });
            }

            verification.User.EmailVerified = true;
            _db.EmailVerifications.Remove(verification);
            await _db.SaveChangesAsync(ct);

            var authResponse = await CreateAuthResponseAsync(verification.User , ct);

            return Ok(ApiResponse<AuthResponse>.Ok(authResponse , "Email verified successfully and logged in."));
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
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;
            if ( string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr , out var userId) )
            {
                return Unauthorized(new { Message = "Unauthorized access." });
            }

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
            var adminEmail = _config["Authentication:Admin:Email"];
            var adminPassword = _config["Authentication:Admin:Password"];

            // Admin defined in appsettings override bypasses database completely
            if (!string.IsNullOrEmpty(adminEmail) && 
                req.Email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase) && 
                req.Password == adminPassword)
            {
                var roles = new List<string> { "admin" };
                var adminToken = _tokenService.CreateAccessToken(Guid.Empty.ToString(), "Super Admin", roles);
                var adminUserDto = new UserDto(
                    UserId: Guid.Empty, 
                    Email: adminEmail, 
                    FirstName: "Super", 
                    LastName: "Admin", 
                    DisplayName: "Super Admin", 
                    Username: "superadmin", 
                    AvatarUrl: null, 
                    emailVerified: true,
                    Roles: roles);
                    
                var adminAuthResponse = new AuthResponse(
                    AccessToken: adminToken, 
                    RefreshToken: "admin-no-refresh", 
                    ExpiresIn: _jwt.AccessTokenMinutes * 60, 
                    User: adminUserDto);
                    
                return Ok(ApiResponse<AuthResponse>.Ok(adminAuthResponse, "Login successful"));
            }

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

            var authResponse = await CreateAuthResponseAsync(user , ct);

            return Ok(ApiResponse<AuthResponse>.Ok(authResponse , "Login successful"));
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

            var authResponse = await CreateAuthResponseAsync(user , ct);

            return Ok(ApiResponse<AuthResponse>.Ok(authResponse , "Login with Google successful"));
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

    [AllowAnonymous]
    [HttpPost("github-login")]
    public async Task<IActionResult> GithubLogin([FromBody] GithubLoginRequest req, CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TMOJ-Auth-App");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.AccessToken);

            var response = await client.GetAsync("https://api.github.com/user", ct);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { Message = "Invalid GitHub Access Token" });
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // GitHub might not return email if it's private, but we need it.
            // In a real app, you might need to call /user/emails
            var email = root.GetProperty("email").GetString()?.ToLowerInvariant();
            var githubId = root.GetProperty("id").GetInt64().ToString();
            var name = root.GetProperty("name").GetString() ?? root.GetProperty("login").GetString();
            var avatarUrl = root.GetProperty("avatar_url").GetString();

            if (string.IsNullOrEmpty(email))
            {
                // Fallback attempt to get private email
                var emailResponse = await client.GetAsync("https://api.github.com/user/emails", ct);
                if (emailResponse.IsSuccessStatusCode)
                {
                    var emailContent = await emailResponse.Content.ReadAsStringAsync(ct);
                    using var emailDoc = JsonDocument.Parse(emailContent);
                    email = emailDoc.RootElement.EnumerateArray()
                        .FirstOrDefault(e => e.GetProperty("primary").GetBoolean())
                        .GetProperty("email").GetString()?.ToLowerInvariant();
                }
            }

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { Message = "Could not retrieve email from GitHub account." });
            }

            var user = await _db.Users
                .Include(u => u.UserProviders)
                .Include(u => u.UserRoleUsers).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            if (user == null)
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student", ct);
                user = new User
                {
                    Email = email,
                    DisplayName = name,
                    AvatarUrl = avatarUrl,
                    Username = email.Split('@')[0] + Random.Shared.Next(1000, 9999).ToString(),
                    EmailVerified = true, // GitHub verified
                    LanguagePreference = "vi",
                    Status = true,
                    UserProviders = new List<UserProvider>(),
                    UserRoleUsers = new List<UserRole>()
                };

                var provider = await _db.Providers.FirstOrDefaultAsync(p => p.ProviderCode == "github", ct);
                if (provider == null)
                {
                    provider = new Provider { ProviderCode = "github", ProviderDisplayName = "GitHub", Enabled = true };
                    _db.Providers.Add(provider);
                }

                user.UserProviders.Add(new UserProvider
                {
                    ProviderId = provider.ProviderId,
                    ProviderSubject = githubId,
                    ProviderEmail = email
                });

                if (studentRole != null) user.UserRoleUsers.Add(new UserRole { RoleId = studentRole.RoleId });

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
            }

            var authResponse = await CreateAuthResponseAsync(user, ct);
            return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Login with GitHub successful"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Internal Server Error during GitHub Login." });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        try
        {
            var hash = _refreshTokenService.HashToken(req.RefreshToken);
            var token = await _db.RefreshTokens
                .Include(t => t.Session).ThenInclude(s => s.User).ThenInclude(u => u.UserRoleUsers).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

            if (token == null || token.ExpireAt < DateTime.UtcNow || token.RevokedAt != null)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token." });
            }

            // Revoke old token
            token.RevokedAt = DateTime.UtcNow;

            // Create new response
            var authResponse = await CreateAuthResponseAsync(token.Session.User, ct);
            return Ok(ApiResponse<AuthResponse>.Ok(authResponse, "Token refreshed successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Error refreshing token." });
        }
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant(), ct);
            if (user == null) return Ok(new { Message = "If an account exists for this email, a reset link has been sent." });

            var verification = new EmailVerification
            {
                UserId = user.UserId,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            _db.EmailVerifications.Add(verification);
            await _db.SaveChangesAsync(ct);

            // TODO: Send reset email
            return Ok(new { Message = "Password reset link sent.", Token = verification.Token });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Error processing forgot password request." });
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        try
        {
            var email = req.Email.ToLowerInvariant();
            var verification = await _db.EmailVerifications
                .FirstOrDefaultAsync(v => v.User.Email == email && v.Token == req.Token, ct);

            if (verification == null || verification.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { Message = "Invalid or expired reset token." });
            }

            var user = await _db.Users.FindAsync(new object[] { verification.UserId }, ct);
            if (user != null)
            {
                user.Password = _passwordHasher.Hash(req.NewPassword);
                _db.EmailVerifications.Remove(verification);
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { Message = "Password reset successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Error resetting password." });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user == null || string.IsNullOrEmpty(user.Password) || !_passwordHasher.Verify(req.CurrentPassword, user.Password))
            {
                return BadRequest(new { Message = "Invalid current password." });
            }

            user.Password = _passwordHasher.Hash(req.NewPassword);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Password changed successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Error changing password." });
        }
    }

    // Administrative Actions
    [Authorize(Roles = "admin")]
    [HttpPut("users/{id}/lock")]
    public async Task<IActionResult> LockAccount(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user == null) return NotFound();
        user.Status = false;
        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = "Account locked." });
    }

    [Authorize(Roles = "admin")]
    [HttpPut("users/{id}/unlock")]
    public async Task<IActionResult> UnlockAccount(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user == null) return NotFound();
        user.Status = true;
        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = "Account unlocked." });
    }

    [Authorize(Roles = "admin")]
    [HttpGet("users/locked")]
    public async Task<IActionResult> ListLockedAccounts(CancellationToken ct)
    {
        var users = await _db.Users.Where(u => u.Status == false)
            .Select(u => new UserProfileResponse(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName, u.Username, u.AvatarUrl, u.EmailVerified, u.Status, u.CreatedAt))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<UserProfileResponse>>.Ok(users, "list of locked accounts"));
    }

    [Authorize(Roles = "admin")]
    [HttpGet("users/unlocked")]
    public async Task<IActionResult> ListUnlockedAccounts(CancellationToken ct)
    {
        var users = await _db.Users.Where(u => u.Status == true)
            .Select(u => new UserProfileResponse(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName, u.Username, u.AvatarUrl, u.EmailVerified, u.Status, u.CreatedAt))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<UserProfileResponse>>.Ok(users, "List of active accounts"));
    }

    [Authorize(Roles = "admin")]
    [HttpPost("users/{id}/assign-role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.UserRoleUsers).FirstOrDefaultAsync(u => u.UserId == id, ct);
        if (user == null) return NotFound();

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == req.RoleCode.ToLowerInvariant(), ct);
        if (role == null) return BadRequest(new { Message = "Role not found." });

        if (!user.UserRoleUsers.Any(ur => ur.RoleId == role.RoleId))
        {
            user.UserRoleUsers.Add(new UserRole { RoleId = role.RoleId });
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { Message = $"Role {req.RoleCode} assigned." });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("users/{id}/roles/{roleCode}")]
    public async Task<IActionResult> RemoveRole(Guid id, string roleCode, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.UserRoleUsers).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id, ct);
        if (user == null) return NotFound();

        var userRole = user.UserRoleUsers.FirstOrDefault(ur => ur.Role.RoleCode == roleCode.ToLowerInvariant());
        if (userRole != null)
        {
            _db.UserRoles.Remove(userRole);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { Message = $"Role {roleCode} removed." });
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user , CancellationToken ct)
    {
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
        if ( !roles.Any() ) roles.Add("user");

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
            AvatarUrl: user.AvatarUrl,
            emailVerified: user.EmailVerified,
            Roles: roles
        );

        return new AuthResponse(
            AccessToken: accessToken ,
            RefreshToken: refreshTokenStr ,
            ExpiresIn: _jwt.AccessTokenMinutes * 60 ,
            User: userDto
        );
    }
}
