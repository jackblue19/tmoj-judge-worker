using Application.UseCases.Auth;
using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Configurations.Auth;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;
using WebAPI.Controllers.v1.Auth;

namespace WebAPI.Controllers.v1.Users;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserController(TmojDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest req ,
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
                Username = req.Username ?? (email.Split('@')[0] + Random.Shared.Next(1000 , 9999).ToString()) ,
                DisplayName = $"{req.FirstName} {req.LastName}" ,
                LanguagePreference = "vi" ,
                Status = true ,
                EmailVerified = true // Admin created users are verified by default
            };

            if ( req.Roles != null && req.Roles.Any() )
            {
                foreach ( var roleCode in req.Roles )
                {
                    var normalizedRoleCode = roleCode.ToLowerInvariant();
                    var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == normalizedRoleCode , ct);
                    if ( role != null )
                    {
                        user.UserRoleUsers.Add(new UserRole { RoleId = role.RoleId });
                    }
                }
            }
            else
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student" , ct);
                if ( studentRole != null )
                {
                    user.UserRoleUsers.Add(new UserRole { RoleId = studentRole.RoleId });
                }
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "User created successfully" , UserId = user.UserId });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while creating the user. Please try again later." });
        }
    }


    [Authorize(Roles = "admin,manager")]
    [HttpGet("list-all")]
    public async Task<IActionResult> ListAll(CancellationToken ct)
    {
        try
        {
            var users = await _db.Users
                .Where(u => u.DeletedAt == null)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new Auth.UserProfileResponse(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.Status,
                    u.CreatedAt
                ))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<Auth.UserProfileResponse>>.Ok(users , "Users list fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching the users list. Please try again later." });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        try
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;
            if ( string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr , out var userId) )
            {
                return Unauthorized(new { Message = "Unauthorized access." });
            }

            var user = await _db.Users
                .Where(u => u.UserId == userId && u.DeletedAt == null)
                .Select(u => new Auth.UserProfileResponse(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.Status,
                    u.CreatedAt
                ))
                .FirstOrDefaultAsync(ct);

            if ( user == null ) return NotFound(new { Message = "User not found." });

            return Ok(ApiResponse<Auth.UserProfileResponse>.Ok(user , "Profile fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching your profile. Please try again later." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
    {
        try
        {
            var user = await _db.Users
                .Where(u => u.UserId == id && u.DeletedAt == null)
                .Select(u => new Auth.UserProfileResponse(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.Status,
                    u.CreatedAt
                ))
                .FirstOrDefaultAsync(ct);

            if ( user == null ) return NotFound(new { Message = "User not found." });

            return Ok(ApiResponse<Auth.UserProfileResponse>.Ok(user , "User profile fetched successfully"));
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while fetching the user profile. Please try again later." });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var user = await _db.Users.FindAsync(new object[] { id }, ct);
            if (user == null) return NotFound(new { Message = "User not found." });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Account deleted successfully." });
        }
        catch ( Exception )
        {
            return StatusCode(500 , new { Message = "An error occurred while deleting the account. Please try again later." });
        }
    }
}
