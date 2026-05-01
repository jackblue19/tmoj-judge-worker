using Application.UseCases.Auth.Commands.ChangePassword;
using Application.UseCases.Auth.Commands.ConfirmEmail;
using Application.UseCases.Auth.Commands.ForgotPassword;
using Application.UseCases.Auth.Commands.Login;
using Application.UseCases.Auth.Commands.Logout;
using Application.UseCases.Auth.Commands.RefreshToken;
using Application.UseCases.Auth.Commands.Register;
using Application.UseCases.Auth.Commands.ResetPassword;
using Application.UseCases.Auth.Commands.SocialLogin;
using Application.UseCases.Auth.Dtos;
using Application.UseCases.Auth.Options;
using Asp.Versioning;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Auth;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly GoogleOptions _google;
    private readonly GithubOptions _github;
    private readonly ITimeLimitedDataProtector _protector;

    public AuthController(
        IMediator mediator,
        IOptions<GoogleOptions> google,
        IOptions<GithubOptions> github,
        IDataProtectionProvider dp)
    {
        _mediator = mediator;
        _google = google.Value;
        _github = github.Value;
        _protector = dp.CreateProtector("github-oauth").ToTimeLimitedDataProtector();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand req, CancellationToken ct)
    {
        try
        {
            var command = req with { IpAddress = IpAddress(), UserAgent = UserAgent() };
            var result = await _mediator.Send(command, ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred during registration.", Error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ConfirmEmailCommand(email, token, IpAddress(), UserAgent()), ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Email verified successfully and logged in."));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred during email verification." });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

            await _mediator.Send(new LogoutCommand(userId.Value), ct);
            return Ok(new { Message = "Logged out successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred during logout." });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand req, CancellationToken ct)
    {
        try
        {
            var command = req with { IpAddress = IpAddress(), UserAgent = UserAgent() };
            var result = await _mediator.Send(command, ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred during login." });
        }
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req, CancellationToken ct)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(req.TokenId,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _google.ClientId } });

            var email = payload.Email.ToLowerInvariant();
            if (_google.AllowedDomains.Any() &&
                !_google.AllowedDomains.Any(d => email.EndsWith($"@{d}", StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { Message = "Login with this email domain is not allowed." });

            var command = new SocialLoginCommand(
                Email: email,
                FirstName: payload.GivenName,
                LastName: payload.FamilyName,
                DisplayName: payload.Name,
                AvatarUrl: payload.Picture,
                ProviderCode: "google",
                ProviderSubject: payload.Subject,
                EmailVerified: payload.EmailVerified,
                IpAddress: IpAddress(),
                UserAgent: UserAgent());

            var result = await _mediator.Send(command, ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login with Google successful"));
        }
        catch (InvalidJwtException)
        {
            return BadRequest(new { Message = "Invalid Google Token" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Internal Server Error during Google Login." });
        }
    }

    [AllowAnonymous]
    [HttpGet("github")]
    public IActionResult GithubOAuth()
    {
        var url = "https://github.com/login/oauth/authorize" +
                  $"?client_id={_github.ClientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(_github.RedirectUri)}" +
                  "&scope=user:email" +
                  "&allow_signup=true";
        return Redirect(url);
    }

    [AllowAnonymous]
    [HttpGet("github/callback")]
    public async Task<IActionResult> GithubCallback(
        [FromQuery] string? code, [FromQuery] string? error, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
            return Redirect($"{_github.FrontendUrl}/login?error=github_denied");

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TMOJ-Auth");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            var tokenResp = await client.PostAsync(
                "https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _github.ClientId,
                    ["client_secret"] = _github.ClientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = _github.RedirectUri
                }), ct);

            if (!tokenResp.IsSuccessStatusCode)
                return Redirect($"{_github.FrontendUrl}/login?error=github_token");

            var tokenContent = await tokenResp.Content.ReadAsStringAsync(ct);
            using var tokenDoc = JsonDocument.Parse(tokenContent);

            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var atEl))
                return Redirect($"{_github.FrontendUrl}/login?error=github_token");

            var accessToken = atEl.GetString();
            if (string.IsNullOrEmpty(accessToken))
                return Redirect($"{_github.FrontendUrl}/login?error=github_token");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var userResp = await client.GetAsync("https://api.github.com/user", ct);
            if (!userResp.IsSuccessStatusCode)
                return Redirect($"{_github.FrontendUrl}/login?error=github_user");

            var userContent = await userResp.Content.ReadAsStringAsync(ct);
            using var userDoc = JsonDocument.Parse(userContent);
            var root = userDoc.RootElement;

            var githubId = root.GetProperty("id").GetInt64().ToString();
            var login = root.GetProperty("login").GetString();
            var name = root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind != JsonValueKind.Null
                ? nameEl.GetString() : null;
            var avatarUrl = root.TryGetProperty("avatar_url", out var avatarEl) && avatarEl.ValueKind != JsonValueKind.Null
                ? avatarEl.GetString() : null;
            string? email = root.TryGetProperty("email", out var emailEl) && emailEl.ValueKind != JsonValueKind.Null
                ? emailEl.GetString()?.ToLowerInvariant() : null;

            if (string.IsNullOrEmpty(email))
            {
                var emailResp = await client.GetAsync("https://api.github.com/user/emails", ct);
                if (emailResp.IsSuccessStatusCode)
                {
                    var emailContent = await emailResp.Content.ReadAsStringAsync(ct);
                    using var emailDoc = JsonDocument.Parse(emailContent);
                    var primary = emailDoc.RootElement.EnumerateArray()
                        .FirstOrDefault(e => e.TryGetProperty("primary", out var p) && p.GetBoolean());
                    if (primary.ValueKind != JsonValueKind.Undefined)
                        email = primary.GetProperty("email").GetString()?.ToLowerInvariant();
                }
            }

            if (string.IsNullOrEmpty(email))
                return Redirect($"{_github.FrontendUrl}/login?error=github_no_email");

            var result = await _mediator.Send(new SocialLoginCommand(
                Email: email,
                FirstName: null,
                LastName: null,
                DisplayName: name ?? login,
                AvatarUrl: avatarUrl,
                ProviderCode: "github",
                ProviderSubject: githubId,
                EmailVerified: true,
                IpAddress: IpAddress(),
                UserAgent: UserAgent()), ct);

            var payload = JsonSerializer.Serialize(new GithubSessionPayload(result, UserAgent() ?? ""));
            var token = _protector.Protect(payload, TimeSpan.FromSeconds(90));

            return Redirect($"{_github.FrontendUrl}/auth/github/success?t={Uri.EscapeDataString(token)}");
        }
        catch
        {
            return Redirect($"{_github.FrontendUrl}/login?error=github_failed");
        }
    }

    [AllowAnonymous]
    [HttpGet("github/session")]
    public IActionResult GithubSession([FromQuery] string t)
    {
        if (string.IsNullOrWhiteSpace(t))
            return BadRequest(new { Message = "Missing token." });

        try
        {
            var payload = _protector.Unprotect(t);
            var session = JsonSerializer.Deserialize<GithubSessionPayload>(payload);
            if (session is null) return BadRequest(new { Message = "Invalid token." });

            if (session.UserAgent != (UserAgent() ?? ""))
                return BadRequest(new { Message = "Token not valid for this client." });

            return Ok(ApiResponse<AuthResponseDto>.Ok(session.Auth, "GitHub login successful"));
        }
        catch
        {
            return BadRequest(new { Message = "Token expired or invalid." });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new RefreshTokenCommand(req.RefreshToken, IpAddress(), UserAgent()), ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
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
            var token = await _mediator.Send(new ForgotPasswordCommand(req.Email), ct);
            return Ok(new { Message = "If an account exists for this email, a reset link has been sent.", Token = token });
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
            await _mediator.Send(new ResetPasswordCommand(req.Email, req.Token, req.NewPassword), ct);
            return Ok(new { Message = "Password reset successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Message = ex.Message });
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
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var token = await _mediator.Send(new ChangePasswordCommand(userId.Value, req.CurrentPassword, req.NewPassword), ct);
            return Ok(new { Message = "Password changed successfully. Please check your email to verify your account.", Token = token });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "Error changing password." });
        }
    }

    private Guid? GetUserId()
    {
        var s = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(s, out var id) ? id : null;
    }

    private string? IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? UserAgent() => Request.Headers["User-Agent"].ToString();
}

file record GithubSessionPayload(AuthResponseDto Auth, string UserAgent);
