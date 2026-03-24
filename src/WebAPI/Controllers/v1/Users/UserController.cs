using Application.Abstractions.Outbound.Services;
using Application.UseCases.Auth;
using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Configurations.Auth;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Controllers.v1.Auth;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Users;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
//[Authorize]
public class UserController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthController> _logger;
    private readonly ICloudinaryService _cloudinary;

    public UserController(TmojDbContext db, IPasswordHasher passwordHasher, ILogger<AuthController> logger, ICloudinaryService cloudinary)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _cloudinary = cloudinary;
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
                DisplayName = $"{req.LastName} {req.FirstName}" ,
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
                .Include(u => u.UserRoleUsers)
                    .ThenInclude(ur => ur.Role)
                .Select(u => new UserDto(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.UserRoleUsers
                        .Select(ur => ur.Role.RoleCode)
                        .ToList()
                ))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<UserDto>>.Ok(
                users,
                "Users list fetched successfully"
            ));
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                Message = "An error occurred while fetching the users list. Please try again later."
            });
        }
    }

    [Authorize]
[HttpGet("me")]
public async Task<IActionResult> GetMe(CancellationToken ct)
{
    try
    {
        var userIdStr =
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { Message = "Unauthorized access." });
        }

        var user = await _db.Users
            .Where(u => u.UserId == userId && u.DeletedAt == null)
            .Select(u => new UserDto(
                u.UserId,
                u.Email,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                u.Username,
                u.AvatarUrl,
                u.EmailVerified,
                u.UserRoleUsers
                    .Select(ur => ur.Role.RoleCode)
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        if (user == null)
            return NotFound(new { Message = "User not found." });

        return Ok(ApiResponse<UserDto>.Ok(
            user,
            "Profile fetched successfully"
        ));
    }
    catch (Exception)
    {
        return StatusCode(500, new
        {
            Message = "An error occurred while fetching your profile. Please try again later."
        });
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
    [HttpGet("role/{roleName}")]
    public async Task<IActionResult> ListAllUserByRole(string roleName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest(new { message = "Role name is required." });

        try
        {
            var normalizedRoleName = roleName.Trim().ToLowerInvariant();

            var role = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleCode.ToLower() == normalizedRoleName, ct);

            if (role == null)
                return NotFound(new { message = "Role name not found." });

            var users = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserRoleUsers.Any(ur => ur.RoleId == role.RoleId))
                 .OrderBy(u => u.DisplayName)
                .Select(u => new UserDto(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.UserRoleUsers.Select(ur => ur.Role.RoleCode).ToList()
                ))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<UserDto>>.Ok(users, "Users fetched successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users by role {RoleName}", roleName);

            return StatusCode(500, new
            {
                message = "An error occurred while fetching users."
            });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/User/import/template
    // Template cho admin thêm sinh viên vào hệ thống
    // ──────────────────────────────────────────
    [Authorize(Roles = "admin")]
    [HttpGet("import/template")]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string>
        {
            "FullName",
            "Email",
            "RollNumber",
            "MemberCode"
        };

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            // Sample row
            worksheet.Cell(2, 1).Value = "Nguyen Van A";
            worksheet.Cell(2, 2).Value = "nguyenva@domain.com";
            worksheet.Cell(2, 3).Value = "SE123456";
            worksheet.Cell(2, 4).Value = "A_NV";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Student_Import_Template.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

        // ──────────────────────────────────────────
        // POST api/v1/User/import
        // Admin import sinh viên hàng loạt từ Excel
        // ──────────────────────────────────────────
        [Authorize(Roles = "admin")]
        [HttpPost("import")]
        public async Task<IActionResult> ImportStudents(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "File is missing." });

            try
            {
                int successCount = 0;
                int failedCount = 0;
                var errors = new List<string>();
                int totalProcessed = 0;

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, ct);
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1); // skip header

                var headerRow = worksheet.Row(1);
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var cell in headerRow.CellsUsed())
                {
                    headers[cell.GetValue<string>().Trim()] = cell.Address.ColumnNumber;
                }

                foreach (var row in rows)
                {
                    totalProcessed++;
                    try
                    {
                        string fullName = GetCellString(row, headers, "FullName");
                        string email = GetCellString(row, headers, "Email");             
                        string rollNumber = GetCellString(row, headers, "RollNumber");
                        string memberCode = GetCellString(row, headers, "MemberCode");

                        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
                        {
                            errors.Add($"Row {row.RowNumber()}: Missing Email or FullName.");
                            failedCount++;
                            continue;
                        }

                        email = email.Trim().ToLowerInvariant();
                        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

                        if (user == null)
                        {
                            var names = SplitFullName(fullName);
                            user = new User
                            {
                                FirstName = names.FirstName,
                                LastName = names.LastName,
                                Email = email,
                                Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                                DisplayName = fullName,
                                RollNumber = rollNumber,
                                MemberCode = memberCode,
                                Status = true,
                                EmailVerified = true,
                                LanguagePreference = "en",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            // Assign student role by default
                            var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student", ct);
                            if (studentRole != null)
                            {
                                user.RoleId = studentRole.RoleId;
                            }

                            _db.Users.Add(user);
                            await _db.SaveChangesAsync(ct);
                            successCount++;
                        }
                        else
                        {
                            // User đã tồn tại — bỏ qua, không tạo mới
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                        failedCount++;
                    }
                }

                var result = new
                {
                    TotalProcessed = totalProcessed,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    Errors = errors
                };

                return Ok(ApiResponse<object>.Ok(result, "Import processed successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
            }
        } 


    private string GetCellString(ClosedXML.Excel.IXLRow row, Dictionary<string, int> headers, string columnName)
    {
        if (headers.TryGetValue(columnName, out int colIdx))
        {
            return row.Cell(colIdx).GetValue<string>()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    private (string LastName, string FirstName) SplitFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("Unknown", "Unknown");

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }

        string lastName = parts[0];
        string firstName = string.Join(" ", parts.Skip(1));
        return (lastName, firstName);
    }

    // ──────────────────────────────────────────
    //  Avatar Upload / Delete
    //  Uses Cloudinary with GUID as the PublicId link
    // ──────────────────────────────────────────

    /// Upload or replace the current user's avatar.
    /// The avatar is stored in Cloudinary and linked by GUID in User.AvatarUrl.
    [Authorize]
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_000_000)] // 5 MB
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { Message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });

        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { Message = "Unauthorized access." });

        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null, ct);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            // If user already has an avatar, replace it; otherwise upload new
            if (!string.IsNullOrEmpty(user.AvatarUrl) && Guid.TryParse(user.AvatarUrl, out var existingAvatarId))
            {
                await _cloudinary.ReplaceAvatarAsync(existingAvatarId, file.OpenReadStream(), ext, ct);
            }
            else
            {
                var avatarId = await _cloudinary.UploadAvatarAsync(file.OpenReadStream(), ext, ct);
                user.AvatarUrl = avatarId.ToString();
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            // Resolve the GUID to a full URL for the response
            var avatarUrl = Guid.TryParse(user.AvatarUrl, out var aid)
                ? _cloudinary.GetAvatarUrl(aid)
                : user.AvatarUrl;

            return Ok(new { Message = "Avatar uploaded successfully.", AvatarUrl = avatarUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while uploading the avatar." });
        }
    }

    /// Delete the current user's avatar from Cloudinary.
    [Authorize]
    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(new { Message = "Unauthorized access." });

        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null, ct);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            if (string.IsNullOrEmpty(user.AvatarUrl))
                return BadRequest(new { Message = "No avatar to delete." });

            if (Guid.TryParse(user.AvatarUrl, out var avatarId))
            {
                await _cloudinary.DeleteAvatarAsync(avatarId, ct);
            }

            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { Message = "Avatar deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while deleting the avatar." });
        }
    }

    private Guid GetUserId()
    {
        var v =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("user_id") ??
            User.FindFirstValue("uid");

        return Guid.TryParse(v, out var id) ? id : Guid.Empty;
    }
}
