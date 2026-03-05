using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers.v1.ProblemManagement.ForeignKeys;

//[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/test")]
[ApiController]
public class AccCrud : ControllerBase
{
    private readonly TmojDbContext _db;

    public AccCrud(TmojDbContext db)
    {
        _db = db;
    }

    // GET: api/test/users?includeDeleted=false
    [HttpGet("users")]
    public async Task<ActionResult<List<UserResponseDto>>> GetAll(
        [FromQuery] bool includeDeleted = false ,
        CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking();

        if ( !includeDeleted )
            query = query.Where(x => x.DeletedAt == null);

        var users = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UserResponseDto
            {
                UserId = x.UserId ,
                FirstName = x.FirstName ,
                LastName = x.LastName ,
                Username = x.Username ,
                Email = x.Email ,
                EmailVerified = x.EmailVerified ,
                AvatarUrl = x.AvatarUrl ,
                DisplayName = x.DisplayName ,
                LanguagePreference = x.LanguagePreference ,
                Status = x.Status ,
                CreatedAt = x.CreatedAt ,
                UpdatedAt = x.UpdatedAt ,
                CreatedBy = x.CreatedBy ,
                DeletedAt = x.DeletedAt ,
                RoleId = x.RoleId
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    //[ApiExplorerSettings(IgnoreApi = true)]
    // GET: api/test/users/{id}
    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<UserResponseDto>> GetById(
        [FromRoute] Guid id ,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(x => x.UserId == id)
            .Select(x => new UserResponseDto
            {
                UserId = x.UserId ,
                FirstName = x.FirstName ,
                LastName = x.LastName ,
                Username = x.Username ,
                Email = x.Email ,
                EmailVerified = x.EmailVerified ,
                AvatarUrl = x.AvatarUrl ,
                DisplayName = x.DisplayName ,
                LanguagePreference = x.LanguagePreference ,
                Status = x.Status ,
                CreatedAt = x.CreatedAt ,
                UpdatedAt = x.UpdatedAt ,
                CreatedBy = x.CreatedBy ,
                DeletedAt = x.DeletedAt ,
                RoleId = x.RoleId
            })
            .FirstOrDefaultAsync(ct);

        if ( user is null )
            return NotFound();

        return Ok(user);
    }

    // POST: api/test/users
    [HttpPost("users")]
    public async Task<ActionResult<UserResponseDto>> Create(
        [FromBody] UserCreateDto dto ,
        CancellationToken ct = default)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users
            .AsNoTracking()
            .AnyAsync(x =>
                x.DeletedAt == null &&
                (x.Username == username || x.Email == email) ,
                ct);

        if ( exists )
            return Conflict(new { message = "Username hoặc Email đã tồn tại." });

        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid() ,
            FirstName = dto.FirstName.Trim() ,
            LastName = dto.LastName.Trim() ,
            Username = username ,
            Email = email ,
            EmailVerified = false ,
            Password = dto.Password ,
            AvatarUrl = dto.AvatarUrl ,
            DisplayName = dto.DisplayName ,
            LanguagePreference = string.IsNullOrWhiteSpace(dto.LanguagePreference) ? "en" : dto.LanguagePreference.Trim() ,
            Status = dto.Status ?? true ,
            CreatedAt = now ,
            UpdatedAt = now ,
            CreatedBy = null ,
            DeletedAt = null ,
            RoleId = dto.RoleId
        };

        _db.Users.Add(user);    //ko bị lỗi thì sẽ thực hiện dòng dưới (nên dùng try-catch để bắt lỗi)
        await _db.SaveChangesAsync(ct); // đoạn này bắt đầu mới truyền từ handler về db

        var res = new UserResponseDto
        {
            UserId = user.UserId ,
            FirstName = user.FirstName ,
            LastName = user.LastName ,
            Username = user.Username ,
            Email = user.Email ,
            EmailVerified = user.EmailVerified ,
            AvatarUrl = user.AvatarUrl ,
            DisplayName = user.DisplayName ,
            LanguagePreference = user.LanguagePreference ,
            Status = user.Status ,
            CreatedAt = user.CreatedAt ,
            UpdatedAt = user.UpdatedAt ,
            CreatedBy = user.CreatedBy ,
            DeletedAt = user.DeletedAt ,
            RoleId = user.RoleId
        };

        return CreatedAtAction(nameof(GetById) , new { id = user.UserId } , res);
    }

    // PUT: api/test/users
    [HttpPut("users")]
    public async Task<ActionResult<UserResponseDto>> Update(
        [FromBody] UserUpdateDto dto ,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == dto.UserId , ct);

        if ( user is null )
            return NotFound();

        if ( user.DeletedAt is not null )
            return Conflict(new { message = "User đã bị xóa (soft delete), không thể update." });

        if ( !string.IsNullOrWhiteSpace(dto.Username) )
        {
            var newUsername = dto.Username.Trim();

            var usernameTaken = await _db.Users
                .AsNoTracking()
                .AnyAsync(x => x.DeletedAt == null && x.UserId != user.UserId && x.Username == newUsername , ct);

            if ( usernameTaken )
                return Conflict(new { message = "Username đã tồn tại." });

            user.Username = newUsername;
        }

        if ( !string.IsNullOrWhiteSpace(dto.Email) )
        {
            var newEmail = dto.Email.Trim().ToLowerInvariant();

            var emailTaken = await _db.Users
                .AsNoTracking()
                .AnyAsync(x => x.DeletedAt == null && x.UserId != user.UserId && x.Email == newEmail , ct);

            if ( emailTaken )
                return Conflict(new { message = "Email đã tồn tại." });

            user.Email = newEmail;
        }

        if ( !string.IsNullOrWhiteSpace(dto.FirstName) )
            user.FirstName = dto.FirstName.Trim();

        if ( !string.IsNullOrWhiteSpace(dto.LastName) )
            user.LastName = dto.LastName.Trim();

        if ( dto.EmailVerified.HasValue )
            user.EmailVerified = dto.EmailVerified.Value;

        if ( dto.Password is not null )
            user.Password = dto.Password;

        if ( dto.AvatarUrl is not null )
            user.AvatarUrl = dto.AvatarUrl;

        if ( dto.DisplayName is not null )
            user.DisplayName = dto.DisplayName;

        if ( !string.IsNullOrWhiteSpace(dto.LanguagePreference) )
            user.LanguagePreference = dto.LanguagePreference.Trim();

        if ( dto.Status.HasValue )
            user.Status = dto.Status.Value;

        if ( dto.RoleId.HasValue )
            user.RoleId = dto.RoleId;

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var res = new UserResponseDto
        {
            UserId = user.UserId ,
            FirstName = user.FirstName ,
            LastName = user.LastName ,
            Username = user.Username ,
            Email = user.Email ,
            EmailVerified = user.EmailVerified ,
            AvatarUrl = user.AvatarUrl ,
            DisplayName = user.DisplayName ,
            LanguagePreference = user.LanguagePreference ,
            Status = user.Status ,
            CreatedAt = user.CreatedAt ,
            UpdatedAt = user.UpdatedAt ,
            CreatedBy = user.CreatedBy ,
            DeletedAt = user.DeletedAt ,
            RoleId = user.RoleId
        };

        return Ok(res);
    }

    // DELETE: api/test/users/{id}
    // soft delete: set DeletedAt + UpdatedAt
    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id ,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == id , ct);

        if ( user is null )
            return NotFound();

        if ( user.DeletedAt is not null )
            return NoContent();

        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}