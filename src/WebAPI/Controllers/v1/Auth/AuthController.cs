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
using Application.Abstractions.Outbound.Services;
using MediatR;
using Application.UseCases.Gamification.EventsHandlers;
using Application.Common.Events;
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
    private readonly IEmailService _emailService;
    private readonly IMediator _mediator;



    public AuthController(
        ITokenService tokenService ,
        IRefreshTokenService refreshTokenService ,
        IPasswordHasher passwordHasher ,
        IOptions<JwtOptions> jwt ,
        IOptions<GoogleOptions> google ,
        IOptions<GithubOptions> github ,
        ILogger<AuthController> logger ,
        TmojDbContext db ,
        IConfiguration config ,
        IEmailService emailService,
        IMediator mediator)

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
        _emailService = emailService;
        _mediator = mediator;
    }

    // [AllowAnonymous]
    // [HttpGet("ping")]
    // public IActionResult Ping() => Ok(new { Message = "pong" });


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

            var emailSettings = _config.GetSection("EmailSettings");
            var template = emailSettings["VerificationEmailTemplate"] ?? "<a href='{LINK}'>Xác nhận Email</a>";

            string GenerateEmailVerificationHtml(string link) =>
                template.Replace("{LINK}", link).Replace("{YEAR}", DateTime.Now.Year.ToString());

            if ( _google.AllowedDomains.Any() && !_google.AllowedDomains.Any(d => email.EndsWith($"@{d}" , StringComparison.OrdinalIgnoreCase)) )
            {
                return BadRequest(new { Message = "Registration with this email domain is not allowed." });
            }

            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            bool IsFptEmail(string eml)
            {
                if ( string.IsNullOrWhiteSpace(eml) ) return false;
                // fe.edu.vn là domain riêng của cán bộ giảng viên FPT
                return eml.ToLowerInvariant().EndsWith("@fe.edu.vn");
            }

            var roleCode = IsFptEmail(email) ? "teacher" : "student";
            var selectedRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == roleCode , ct);
            if ( selectedRole == null ) throw new Exception("Role not found");

            if ( existingUser != null )
            {
                if ( existingUser.EmailVerified )
                {
                    return BadRequest(new { Message = "Email already exists and verified" });
                }

                // Ghi đè thông tin đăng ký lên tài khoản import chưa có password
                // HOẶC gửi lại email xác nhận cho tài khoản đăng ký nhưng chưa kích hoạt
                existingUser.FirstName = req.FirstName;
                existingUser.LastName = req.LastName;
                existingUser.DisplayName = $"{req.LastName} {req.FirstName}";
                existingUser.AvatarUrl = req.Avatar;
                existingUser.Password = _passwordHasher.Hash(req.Password);
                existingUser.LanguagePreference = "vi";
                existingUser.EmailVerified = false;

                if ( existingUser.RoleId == null )
                {
                    existingUser.RoleId = selectedRole.RoleId;
                }

                var oldVerifications = await _db.EmailVerifications.Where(v => v.UserId == existingUser.UserId).ToListAsync(ct);
                _db.EmailVerifications.RemoveRange(oldVerifications);

                var verification = new EmailVerification
                {
                    UserId = existingUser.UserId,
                    Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };
                
                _db.EmailVerifications.Add(verification);
                await _db.SaveChangesAsync(ct);

                var confirmLink = $"https://api.tmoj.id.vn/api/v1/Auth/confirm-email?email={Uri.EscapeDataString(existingUser.Email)}&token={Uri.EscapeDataString(verification.Token)}";

                var subject = "Xác nhận địa chỉ email - TMOJ";
                var body = GenerateEmailVerificationHtml(confirmLink);
                await _emailService.SendEmailAsync(existingUser.Email, subject, body, ct);

                // Tạm thời tự động gọi để bypass việc confirm email (DEV Mode)
                return await ConfirmEmail(existingUser.Email, verification.Token, ct);
            }

            var user = new User
            {
                FirstName = req.FirstName ,
                LastName = req.LastName ,
                Email = email ,
                Password = _passwordHasher.Hash(req.Password) ,
                Username = email.Split('@')[0] + Random.Shared.Next(1000 , 9999).ToString() ,
                DisplayName = $"{req.LastName} {req.FirstName}" ,
                AvatarUrl = req.Avatar ,
                LanguagePreference = "vi" ,
                Status = true ,
                EmailVerified = false ,
                RoleId = selectedRole.RoleId
            };

            var newVerification = new EmailVerification
            {
                User = user ,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) ,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _db.Users.Add(user);
            _db.EmailVerifications.Add(newVerification);
            await _db.SaveChangesAsync(ct);

            var newConfirmLink = $"https://api.tmoj.id.vn/api/v1/Auth/confirm-email?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(newVerification.Token)}";

            var subjectNew = "Xác nhận địa chỉ email - TMOJ";
            var bodyNew = GenerateEmailVerificationHtml(newConfirmLink);
            await _emailService.SendEmailAsync(user.Email, subjectNew, bodyNew, ct);

            // Tạm thời tự động gọi để bypass việc confirm email (DEV Mode)
            return await ConfirmEmail(user.Email, newVerification.Token, ct);
        }
        catch ( Exception ex )
        {
            _logger.LogError(ex, "Register Error");
            return StatusCode(500 , new { 
                Message = "An error occurred during registration. Please try again later.", 
                Error = ex.Message,
                Inner = ex.InnerException?.Message,
                StackTrace = ex.StackTrace 
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string email ,
        [FromQuery] string token ,
        CancellationToken ct)
    {
        try
        {
            if ( string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) )
            {
                return BadRequest(new { Message = "Email and token are required." });
            }

            var emailLower = email.ToLowerInvariant();
            var verification = await _db.EmailVerifications
                .Include(v => v.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(v => v.User.Email == emailLower && v.Token == token , ct);

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
            var email = req.Email.ToLowerInvariant();
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            if ( user == null || string.IsNullOrEmpty(user.Password) || !_passwordHasher.Verify(req.Password , user.Password) )
            {
                return BadRequest(new { Message = "Invalid email or password" });
            }

            if ( !user.EmailVerified )
            {
                return BadRequest(new { Message = "Please verify your email before logging in." });
            }

            if ( !user.Status )
            {
                return BadRequest(new { Message = "Your account has been locked." });
            }

            var authResponse = await CreateAuthResponseAsync(user, ct);
            await _mediator.Publish(new DailyLoginEvent(user.UserId), ct);

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
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            var provider = await _db.Providers.FirstOrDefaultAsync(p => p.ProviderCode == "google" , ct);
            if ( provider == null )
            {
                provider = new Provider
                {
                    ProviderCode = "google" ,
                    ProviderDisplayName = "Google" ,
                    Enabled = true
                };
                _db.Providers.Add(provider);
                await _db.SaveChangesAsync(ct);
            }

            if ( user == null )
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student" , ct);

                user = new User
                {
                    Email = email ,
                    FirstName = payload.GivenName ?? "" ,
                    LastName = payload.FamilyName ?? "" ,
                    DisplayName = payload.Name ,
                    AvatarUrl = payload.Picture ,
                    Username = email.Split('@')[0] + Random.Shared.Next(1000 , 9999).ToString() ,
                    EmailVerified = payload.EmailVerified ,
                    LanguagePreference = "vi" ,
                    Status = true ,
                    UserProviders = new List<UserProvider>()
                };

                user.UserProviders.Add(new UserProvider
                {
                    ProviderId = provider.ProviderId ,
                    ProviderSubject = payload.Subject ,
                    ProviderEmail = email ,
                });

                if ( studentRole != null )
                {
                    user.RoleId = studentRole.RoleId;
                }

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                // Nếu User đã tồn tại (ví dụ import từ Excel) nhưng chưa liên kết Google
                if (!user.UserProviders.Any(p => p.ProviderId == provider.ProviderId))
                {
                    user.UserProviders.Add(new UserProvider
                    {
                        ProviderId = provider.ProviderId,
                        ProviderSubject = payload.Subject,
                        ProviderEmail = email,
                    });

                    // Cập nhật thông tin bổ sung nếu đang bị trống do import
                    if (string.IsNullOrEmpty(user.AvatarUrl)) user.AvatarUrl = payload.Picture;
                    if (string.IsNullOrEmpty(user.FirstName)) user.FirstName = payload.GivenName ?? "";
                    if (string.IsNullOrEmpty(user.LastName)) user.LastName = payload.FamilyName ?? "";
                    if (string.IsNullOrEmpty(user.DisplayName)) user.DisplayName = payload.Name ?? "";
                    
                    if (payload.EmailVerified) user.EmailVerified = true;

                    await _db.SaveChangesAsync(ct);
                }
            }

            var authResponse = await CreateAuthResponseAsync(user, ct);
            await _mediator.Publish(new DailyLoginEvent(user.UserId), ct);

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
    public async Task<IActionResult> GithubLogin([FromBody] GithubLoginRequest req , CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent" , "TMOJ-Auth-App");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer" , req.AccessToken);

            var response = await client.GetAsync("https://api.github.com/user" , ct);
            if ( !response.IsSuccessStatusCode )
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

            if ( string.IsNullOrEmpty(email) )
            {
                // Fallback attempt to get private email
                var emailResponse = await client.GetAsync("https://api.github.com/user/emails" , ct);
                if ( emailResponse.IsSuccessStatusCode )
                {
                    var emailContent = await emailResponse.Content.ReadAsStringAsync(ct);
                    using var emailDoc = JsonDocument.Parse(emailContent);
                    email = emailDoc.RootElement.EnumerateArray()
                        .FirstOrDefault(e => e.GetProperty("primary").GetBoolean())
                        .GetProperty("email").GetString()?.ToLowerInvariant();
                }
            }

            if ( string.IsNullOrEmpty(email) )
            {
                return BadRequest(new { Message = "Could not retrieve email from GitHub account." });
            }

            var user = await _db.Users
                .Include(u => u.UserProviders)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email , ct);

            var provider = await _db.Providers.FirstOrDefaultAsync(p => p.ProviderCode == "github" , ct);
            if ( provider == null )
            {
                provider = new Provider { ProviderCode = "github" , ProviderDisplayName = "GitHub" , Enabled = true };
                _db.Providers.Add(provider);
                await _db.SaveChangesAsync(ct);
            }

            if ( user == null )
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student" , ct);
                user = new User
                {
                    Email = email ,
                    DisplayName = name ,
                    AvatarUrl = avatarUrl ,
                    Username = email.Split('@')[0] + Random.Shared.Next(1000 , 9999).ToString() ,
                    EmailVerified = true , // GitHub verified
                    LanguagePreference = "vi" ,
                    Status = true ,
                    UserProviders = new List<UserProvider>()
                };

                user.UserProviders.Add(new UserProvider
                {
                    ProviderId = provider.ProviderId ,
                    ProviderSubject = githubId ,
                    ProviderEmail = email
                });

                if ( studentRole != null ) 
                {
                    user.RoleId = studentRole.RoleId;
                }

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                if (!user.UserProviders.Any(p => p.ProviderId == provider.ProviderId))
                {
                    user.UserProviders.Add(new UserProvider
                    {
                        ProviderId = provider.ProviderId,
                        ProviderSubject = githubId,
                        ProviderEmail = email
                    });

                    if (string.IsNullOrEmpty(user.AvatarUrl)) user.AvatarUrl = avatarUrl;
                    if (string.IsNullOrEmpty(user.DisplayName)) user.DisplayName = name ?? "";
                    user.EmailVerified = true;

                    await _db.SaveChangesAsync(ct);
                }
            }

            var authResponse = await CreateAuthResponseAsync(user, ct);
             await _mediator.Publish(new DailyLoginEvent(user.UserId), ct);
            return Ok(ApiResponse<AuthResponse>.Ok(authResponse , "Login with GitHub successful"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "Internal Server Error during GitHub Login." });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest req , CancellationToken ct)
    {
        try
        {
            var hash = _refreshTokenService.HashToken(req.RefreshToken);
            var token = await _db.RefreshTokens
                .Include(t => t.Session).ThenInclude(s => s.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(t => t.TokenHash == hash , ct);

            if ( token == null || token.ExpireAt < DateTime.UtcNow || token.RevokedAt != null )
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token." });
            }

            // Revoke old token
            token.RevokedAt = DateTime.UtcNow;

            // Create new response
            var authResponse = await CreateAuthResponseAsync(token.Session.User , ct);
            return Ok(ApiResponse<AuthResponse>.Ok(authResponse , "Token refreshed successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "Error refreshing token." });
        }
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req , CancellationToken ct)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLowerInvariant() , ct);
            if ( user == null ) return Ok(new { Message = "If an account exists for this email, a reset link has been sent." });

            var verification = new EmailVerification
            {
                UserId = user.UserId ,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) ,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            _db.EmailVerifications.Add(verification);
            await _db.SaveChangesAsync(ct);

            // localhost:3000 là bên front end
            var resetLink = $"http://api.tmoj.id.vn/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(verification.Token)}";

            var emailSettings = _config.GetSection("EmailSettings");
            var template = emailSettings["ForgotPasswordEmailTemplate"] ?? "<a href='{LINK}'>Khôi phục mật khẩu</a>";
            var body = template.Replace("{LINK}", resetLink).Replace("{YEAR}", DateTime.Now.Year.ToString());

            await _emailService.SendEmailAsync(user.Email, "Khôi phục mật khẩu - TMOJ", body, ct);

            return Ok(new { Message = "Password reset link sent." , Token = verification.Token });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "Error processing forgot password request." });
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req , CancellationToken ct)
    {
        try
        {
            var email = req.Email.ToLowerInvariant();
            var verification = await _db.EmailVerifications
                .FirstOrDefaultAsync(v => v.User.Email == email && v.Token == req.Token , ct);

            if ( verification == null || verification.ExpiresAt < DateTime.UtcNow )
            {
                return BadRequest(new { Message = "Invalid or expired reset token." });
            }

            var user = await _db.Users.FindAsync(new object[] { verification.UserId } , ct);
            if ( user != null )
            {
                user.Password = _passwordHasher.Hash(req.NewPassword);
                user.EmailVerified = true;
                _db.EmailVerifications.Remove(verification);
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { Message = "Password reset successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "Error resetting password." });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req , CancellationToken ct)
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if ( string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr , out var userId) ) return Unauthorized();

            var user = await _db.Users.FindAsync(new object[] { userId } , ct);
            if ( user == null || string.IsNullOrEmpty(user.Password) || !_passwordHasher.Verify(req.CurrentPassword , user.Password) )
            {
                return BadRequest(new { Message = "Invalid current password." });
            }

            user.Password = _passwordHasher.Hash(req.NewPassword);
            user.EmailVerified = false;

            var oldVerifications = await _db.EmailVerifications.Where(v => v.UserId == user.UserId).ToListAsync(ct);
            _db.EmailVerifications.RemoveRange(oldVerifications);

            var verification = new EmailVerification
            {
                UserId = user.UserId ,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) ,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            _db.EmailVerifications.Add(verification);
            await _db.SaveChangesAsync(ct);

            var confirmLink = $"https://localhost:7210/api/v1/Auth/confirm-email?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(verification.Token)}";

            var emailSettings = _config.GetSection("EmailSettings");
            var template = emailSettings["ChangePasswordEmailTemplate"] ?? "<a href='{LINK}'>Xác nhận Email</a>";
            var body = template.Replace("{LINK}", confirmLink).Replace("{YEAR}", DateTime.Now.Year.ToString());

            await _emailService.SendEmailAsync(user.Email, "Xác nhận địa chỉ email - TMOJ", body, ct);

            return Ok(new { Message = "Password changed successfully. Please check your email to verify your account." , Token = verification.Token });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "Error changing password." });
        }
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

        // 1 User = 1 Role
        var roleName = user.Role?.RoleCode ?? "user";

        var accessToken = _tokenService.CreateAccessToken(
            user.UserId.ToString() ,
            user.DisplayName ?? user.Username ,
            new List<string> { roleName });

        var userDto = new UserDto(
            UserId: user.UserId ,
            Email: user.Email ,
            FirstName: user.FirstName ,
            LastName: user.LastName ,
            DisplayName: user.DisplayName ,
            Username: user.Username ,
            AvatarUrl: user.AvatarUrl ,
            emailVerified: user.EmailVerified ,
            Role: roleName
        );

        return new AuthResponse(
            AccessToken: accessToken ,
            RefreshToken: refreshTokenStr ,
            ExpiresIn: _jwt.AccessTokenMinutes * 60 ,
            User: userDto
        );
    }
}
