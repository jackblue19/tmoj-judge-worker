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
using WebAPI.Controllers.v1.ClassManagement;
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
    private readonly ICloudinaryService _cloudinary;

    public UserController(TmojDbContext db, IPasswordHasher passwordHasher, ICloudinaryService cloudinary)
    {
        _db = db;
        _passwordHasher = passwordHasher;
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

            // 1 User = 1 Role (use User.RoleId directly)
            Guid? roleId = null;
            if ( req.Roles != null && req.Roles.Any() )
            {
                // Take the first role (1:1 relationship)
                var roleCode = req.Roles.First().ToLowerInvariant();
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
                if (role != null)
                    roleId = role.RoleId;
            }
            
            if (roleId == null)
            {
                var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student" , ct);
                if ( studentRole != null )
                    roleId = studentRole.RoleId;
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
                EmailVerified = true,
                RoleId = roleId
            };

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
                .Include(u => u.Role)
                .Select(u => new UserDto(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.RollNumber,
                    u.MemberCode,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.Role != null ? u.Role.RoleCode : null
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
                .Include(u => u.Role)
                .Select(u => new UserDto(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.RollNumber,
                    u.MemberCode,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.Role != null ? u.Role.RoleCode : null
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

    // ──────────────────────────────────────────
    // GET api/v1/user/students/{id}  →  Student profile with joined classes (filter by semester/subject)
    // ──────────────────────────────────────────
    [HttpGet("students/{id:guid}")]
    public async Task<IActionResult> GetStudentDetail(
        Guid id,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        CancellationToken ct)
    {
        try
        {
            var student = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.UserId == id && u.DeletedAt == null)
                .Select(u => new UserDto(
                    u.UserId, u.Email, u.FirstName, u.LastName,
                    u.DisplayName, u.Username, u.RollNumber, u.MemberCode,
                    u.AvatarUrl, u.EmailVerified,
                    u.Role != null ? u.Role.RoleCode : null))
                .FirstOrDefaultAsync(ct);

            if (student == null)
                return NotFound(new { Message = "Student not found." });

            var query = _db.ClassMembers.AsNoTracking()
                .Include(m => m.ClassSemester).ThenInclude(cs => cs.Semester)
                .Include(m => m.ClassSemester).ThenInclude(cs => cs.Subject)
                .Include(m => m.ClassSemester).ThenInclude(cs => cs.Teacher)
                .Include(m => m.ClassSemester).ThenInclude(cs => cs.ClassMembers)
                .Where(m => m.UserId == id && m.IsActive);

            if (semesterId.HasValue)
                query = query.Where(m => m.ClassSemester.SemesterId == semesterId.Value);
            if (subjectId.HasValue)
                query = query.Where(m => m.ClassSemester.SubjectId == subjectId.Value);

            var memberships = await query.ToListAsync(ct);

            var classes = memberships
                .Where(m => m.ClassSemester != null && m.ClassSemester.Semester != null && m.ClassSemester.Subject != null)
                .OrderByDescending(m => m.ClassSemester.CreatedAt)
                .Select(m => new ClassInstanceInfo(
                    m.ClassSemester.Id,
                    m.ClassSemester.Semester.SemesterId, m.ClassSemester.Semester.Code,
                    m.ClassSemester.Subject.SubjectId, m.ClassSemester.Subject.Code,
                    m.ClassSemester.Subject.Name, m.ClassSemester.Subject.Description,
                    m.ClassSemester.Semester.StartAt, m.ClassSemester.Semester.EndAt,
                    null, null, m.ClassSemester.CreatedAt,
                    m.ClassSemester.Teacher != null
                        ? new ClassTeacherInfo(
                            m.ClassSemester.Teacher.UserId,
                            m.ClassSemester.Teacher.DisplayName,
                            m.ClassSemester.Teacher.Email,
                            m.ClassSemester.Teacher.AvatarUrl)
                        : null,
                    m.ClassSemester.ClassMembers.Count(cm => cm.IsActive)))
                .ToList();

            var result = new StudentProfileWithClassesResponse(student, classes, classes.Count);
            return Ok(ApiResponse<StudentProfileWithClassesResponse>.Ok(result, "Student profile fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the student profile." });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/user/teachers/{id}  →  Teacher detail with subjects and taught classes (filter by semester/subject)
    // ──────────────────────────────────────────
    [HttpGet("teachers/{id:guid}")]
    public async Task<IActionResult> GetTeacherDetail(
        Guid id,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        CancellationToken ct)
    {
        try
        {
            var teacher = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.UserId == id && u.DeletedAt == null)
                .Select(u => new UserDto(
                    u.UserId, u.Email, u.FirstName, u.LastName,
                    u.DisplayName, u.Username, u.RollNumber, u.MemberCode,
                    u.AvatarUrl, u.EmailVerified,
                    u.Role != null ? u.Role.RoleCode : null))
                .FirstOrDefaultAsync(ct);

            if (teacher == null)
                return NotFound(new { Message = "Teacher not found." });

            var query = _db.ClassSemesters.AsNoTracking()
                .Include(cs => cs.Semester)
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.ClassMembers)
                .Where(cs => cs.TeacherId == id);

            if (semesterId.HasValue)
                query = query.Where(cs => cs.SemesterId == semesterId.Value);
            if (subjectId.HasValue)
                query = query.Where(cs => cs.SubjectId == subjectId.Value);

            var classSemesters = await query.ToListAsync(ct);

            var classes = classSemesters
                .Where(cs => cs.Semester != null && cs.Subject != null)
                .OrderByDescending(cs => cs.CreatedAt)
                .Select(cs => new ClassInstanceInfo(
                    cs.Id,
                    cs.Semester.SemesterId, cs.Semester.Code,
                    cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                    cs.Semester.StartAt, cs.Semester.EndAt,
                    null, null, cs.CreatedAt,
                    cs.Teacher != null
                        ? new ClassTeacherInfo(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl)
                        : null,
                    cs.ClassMembers.Count(m => m.IsActive)))
                .ToList();

            var subjects = classSemesters
                .Where(cs => cs.Subject != null)
                .GroupBy(cs => cs.Subject.SubjectId)
                .Select(g => new TeacherSubjectInfo(
                    g.Key,
                    g.First().Subject.Code,
                    g.First().Subject.Name,
                    g.First().Subject.Description,
                    g.Count()))
                .OrderBy(s => s.Code)
                .ToList();

            var result = new TeacherDetailResponse(teacher, subjects, classes, classes.Count);
            return Ok(ApiResponse<TeacherDetailResponse>.Ok(result, "Teacher detail fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the teacher detail." });
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

            // Query users via direct User.RoleId (1:1 with Role)
            var users = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.RoleId == role.RoleId)
                .OrderBy(u => u.DisplayName)
                .Select(u => new UserDto(
                    u.UserId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.Username,
                    u.RollNumber,
                    u.MemberCode,
                    u.AvatarUrl,
                    u.EmailVerified,
                    u.Role != null ? u.Role.RoleCode : null
                ))
                .ToListAsync(ct);

            return Ok(ApiResponse<List<UserDto>>.Ok(users, "Users fetched successfully"));
        }
        catch (Exception)
        {
            //_logger.LogError(ex, "Error fetching users by role {RoleName}", roleName);

            return StatusCode(500, new
            {
                message = "An error occurred while fetching users."
            });
        }
    }

    // ──────────────────────────────────────────
    // GET api/v1/User/import/template
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

                        // Use User.RoleId directly (1:1 with Role)
                        Guid? studentRoleId = null;
                        var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "student", ct);
                        if (studentRole != null)
                            studentRoleId = studentRole.RoleId;

                        // Default password = MemberCode, fallback to RollNumber, then email prefix
                        var defaultPassword = !string.IsNullOrWhiteSpace(memberCode) ? memberCode.Trim()
                            : !string.IsNullOrWhiteSpace(rollNumber) ? rollNumber.Trim()
                            : email.Split('@')[0];

                        user = new User
                        {
                            FirstName = names.FirstName,
                            LastName = names.LastName,
                            Email = email,
                            Password = _passwordHasher.Hash(defaultPassword),
                            Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                            DisplayName = fullName,
                            RollNumber = rollNumber,
                            MemberCode = memberCode,
                            Status = true,
                            EmailVerified = true,
                            LanguagePreference = "en",
                            RoleId = studentRoleId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

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
    // ──────────────────────────────────────────

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

            if (TryGetAvatarIdFromUrl(user.AvatarUrl, out var existingAvatarId))
            {
                await _cloudinary.ReplaceAvatarAsync(existingAvatarId, file.OpenReadStream(), ext, ct);
                user.AvatarUrl = _cloudinary.GetAvatarUrl(existingAvatarId);
            }
            else
            {
                var avatarId = await _cloudinary.UploadAvatarAsync(file.OpenReadStream(), ext, ct);
                user.AvatarUrl = _cloudinary.GetAvatarUrl(avatarId);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(new { AvatarUrl = user.AvatarUrl }, "Avatar uploaded successfully."));
        }
        catch (Exception)
        {
            //_logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while uploading the avatar." });
        }
    }

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

            if (TryGetAvatarIdFromUrl(user.AvatarUrl, out var avatarId))
            {
                await _cloudinary.DeleteAvatarAsync(avatarId, ct);
            }

            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<object>.Ok(null!, "Avatar deleted successfully."));
        }
        catch (Exception)
        {
            //_logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while deleting the avatar." });
        }
    }

    // ──────────────────────────────────────────
    // Administrative Actions (Moved from AuthController)
    // ──────────────────────────────────────────

    [Authorize(Roles = "admin")]
    [HttpPut("{id}/lock")]
    public async Task<IActionResult> LockAccount(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user == null) return NotFound();
        user.Status = false;
        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = "Account locked." });
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> UnlockAccount(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user == null) return NotFound();
        user.Status = true;
        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = "Account unlocked." });
    }

    [Authorize(Roles = "admin")]
    [HttpGet("locked")]
    public async Task<IActionResult> ListLockedAccounts(CancellationToken ct)
    {
        var users = await _db.Users.Where(u => u.Status == false)
            .Select(u => new Auth.UserProfileResponse(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName, u.Username, u.AvatarUrl, u.EmailVerified, u.Status, u.CreatedAt))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<Auth.UserProfileResponse>>.Ok(users, "List of locked accounts"));
    }

    [Authorize(Roles = "admin")]
    [HttpGet("unlocked")]
    public async Task<IActionResult> ListUnlockedAccounts(CancellationToken ct)
    {
        var users = await _db.Users.Where(u => u.Status == true)
            .Select(u => new Auth.UserProfileResponse(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName, u.Username, u.AvatarUrl, u.EmailVerified, u.Status, u.CreatedAt))
            .ToListAsync(ct);
        return Ok(ApiResponse<List<Auth.UserProfileResponse>>.Ok(users, "List of active accounts"));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id, ct);
        if (user == null) return NotFound(new { Message = "User not found." });

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == req.RoleCode.ToLowerInvariant(), ct);
        if (role == null) return BadRequest(new { Message = "Role not found." });

        if (user.RoleId != role.RoleId)
        {
            user.RoleId = role.RoleId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { Message = $"Role {req.RoleCode} assigned." });
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

    private bool TryGetAvatarIdFromUrl(string? avatarUrl, out Guid avatarId)
    {
        avatarId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(avatarUrl)) return false;

        if (Guid.TryParse(avatarUrl, out avatarId)) return true;

        var fileName = avatarUrl.Split('/').LastOrDefault();
        if (fileName != null)
        {
            var idString = Path.GetFileNameWithoutExtension(fileName);
            return Guid.TryParse(idString, out avatarId);
        }

        return false;
    }
}
