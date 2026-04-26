using Application.UseCases.Users.Commands.AdminUpdateUser;
using Application.UseCases.Users.Commands.AssignRole;
using Application.UseCases.Users.Commands.CreateUser;
using Application.UseCases.Users.Commands.DeleteAvatar;
using Application.UseCases.Users.Commands.DeleteUser;
using Application.UseCases.Users.Commands.ImportStudents;
using Application.UseCases.Users.Commands.SetAccountStatus;
using Application.UseCases.Users.Commands.UpdateMyProfile;
using Application.UseCases.Users.Commands.UploadAvatar;
using Application.UseCases.Users.Dtos;
using Application.UseCases.Users.Queries.GetActiveUsers;
using Application.UseCases.Users.Queries.GetMe;
using Application.UseCases.Users.Queries.GetStudentDetail;
using Application.UseCases.Users.Queries.GetTeacherDetail;
using Application.UseCases.Users.Queries.GetUserByEmail;
using Application.UseCases.Users.Queries.GetUserProfile;
using Application.UseCases.Users.Queries.ListAllUsers;
using Application.UseCases.Users.Queries.ListUsersByRole;
using Application.UseCases.Users.Queries.ListUsersByStatus;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Models.Common;

namespace WebAPI.Controllers.v1.Users;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand req, CancellationToken ct)
    {
        try
        {
            var userId = await _mediator.Send(req, ct);
            return Ok(new { Message = "User created successfully", UserId = userId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the user." });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet("list-all")]
    public async Task<IActionResult> ListAll(CancellationToken ct)
    {
        try
        {
            var users = await _mediator.Send(new ListAllUsersQuery(), ct);
            return Ok(ApiResponse<List<UserDto>>.Ok(users, "Users list fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the users list." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

        try
        {
            var user = await _mediator.Send(new GetMeQuery(userId.Value), ct);
            if (user == null) return NotFound(new { Message = "User not found." });
            return Ok(ApiResponse<UserDto>.Ok(user, "Profile fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching your profile." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
    {
        try
        {
            var user = await _mediator.Send(new GetUserProfileQuery(id), ct);
            if (user == null) return NotFound(new { Message = "User not found." });
            return Ok(ApiResponse<UserProfileDto>.Ok(user, "User profile fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the user profile." });
        }
    }

    [HttpGet("students/{id:guid}")]
    public async Task<IActionResult> GetStudentDetail(
        Guid id,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetStudentDetailQuery(id, semesterId, subjectId), ct);
            if (result == null) return NotFound(new { Message = "Student not found." });
            return Ok(ApiResponse<StudentProfileWithClassesDto>.Ok(result, "Student profile fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the student profile." });
        }
    }

    [HttpGet("teachers/{id:guid}")]
    public async Task<IActionResult> GetTeacherDetail(
        Guid id,
        [FromQuery] Guid? semesterId,
        [FromQuery] Guid? subjectId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetTeacherDetailQuery(id, semesterId, subjectId), ct);
            if (result == null) return NotFound(new { Message = "Teacher not found." });
            return Ok(ApiResponse<TeacherDetailDto>.Ok(result, "Teacher detail fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching the teacher detail." });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteUserCommand(id), ct);
            return Ok(new { Message = "Account deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the account." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        try
        {
            var users = await _mediator.Send(new GetActiveUsersQuery(), ct);
            return Ok(ApiResponse<List<SimpleUserDto>>.Ok(users, "Users fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching users." });
        }
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required." });

        try
        {
            var user = await _mediator.Send(new GetUserByEmailQuery(email), ct);
            if (user == null) return NotFound(new { message = "User not found." });
            return Ok(ApiResponse<SimpleUserDto>.Ok(user, "User fetched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching the user." });
        }
    }

    [HttpGet("role/{roleName}")]
    public async Task<IActionResult> ListAllUserByRole(string roleName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest(new { message = "Role name is required." });

        try
        {
            var users = await _mediator.Send(new ListUsersByRoleQuery(roleName), ct);
            return Ok(ApiResponse<List<UserDto>>.Ok(users, "Users fetched successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching users." });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpGet("import/template")]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Template");

            var headers = new List<string> { "FullName", "Email", "RollNumber", "MemberCode" };
            for (int i = 0; i < headers.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            worksheet.Cell(2, 1).Value = "Nguyen Van A";
            worksheet.Cell(2, 2).Value = "nguyenva@domain.com";
            worksheet.Cell(2, 3).Value = "SE123456";
            worksheet.Cell(2, 4).Value = "A_NV";
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Student_Import_Template.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while generating template: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost("import")]
    public async Task<IActionResult> ImportStudents(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is missing." });

        try
        {
            var students = ParseImportFile(file);
            var result = await _mediator.Send(new ImportStudentsCommand(students), ct);
            return Ok(ApiResponse<object>.Ok(new
            {
                result.TotalProcessed,
                result.SuccessCount,
                result.FailedCount,
                result.Errors
            }, "Import processed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while importing: " + ex.Message });
        }
    }

    [Authorize]
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is required." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { Message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });

        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

        try
        {
            var avatarUrl = await _mediator.Send(new UploadAvatarCommand(userId.Value, file.OpenReadStream(), ext), ct);
            return Ok(ApiResponse<object>.Ok(new { AvatarUrl = avatarUrl }, "Avatar uploaded successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while uploading the avatar." });
        }
    }

    [Authorize]
    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

        try
        {
            await _mediator.Send(new DeleteAvatarCommand(userId.Value), ct);
            return Ok(ApiResponse<object>.Ok(null!, "Avatar deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the avatar." });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}/lock")]
    public async Task<IActionResult> LockAccount(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new SetAccountStatusCommand(id, false), ct);
            return Ok(new { Message = "Account locked." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}/unlock")]
    public async Task<IActionResult> UnlockAccount(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new SetAccountStatusCommand(id, true), ct);
            return Ok(new { Message = "Account unlocked." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Roles = "admin")]
    [HttpGet("locked")]
    public async Task<IActionResult> ListLockedAccounts(CancellationToken ct)
    {
        var users = await _mediator.Send(new ListUsersByStatusQuery(false), ct);
        return Ok(ApiResponse<List<UserProfileDto>>.Ok(users, "List of locked accounts"));
    }

    [Authorize(Roles = "admin")]
    [HttpGet("unlocked")]
    public async Task<IActionResult> ListUnlockedAccounts(CancellationToken ct)
    {
        var users = await _mediator.Send(new ListUsersByStatusQuery(true), ct);
        return Ok(ApiResponse<List<UserProfileDto>>.Ok(users, "List of active accounts"));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AssignRoleCommand(id, req.RoleCode), ct);
            return Ok(new { Message = $"Role {req.RoleCode} assigned." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> AdminUpdateUser(Guid id, [FromBody] AdminUpdateUserRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AdminUpdateUserCommand(
                id, req.FirstName, req.LastName, req.Username,
                req.DisplayName, req.Password, req.RoleCode, req.Status), ct);
            return Ok(ApiResponse<object>.Ok(new { UserId = id }, "User updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the user." });
        }
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest req, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { Message = "Unauthorized access." });

        try
        {
            await _mediator.Send(new UpdateMyProfileCommand(
                userId.Value, req.FirstName, req.LastName, req.DisplayName, req.Password), ct);
            return Ok(ApiResponse<object>.Ok(new { UserId = userId }, "Profile updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { Message = "An error occurred while updating your profile." });
        }
    }

    private Guid? GetUserId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(v, out var id) ? id : null;
    }

    private static List<ImportStudentItem> ParseImportFile(IFormFile file)
    {
        var items = new List<ImportStudentItem>();
        using var stream = new MemoryStream();
        file.CopyTo(stream);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in worksheet.Row(1).CellsUsed())
            headers[cell.GetValue<string>().Trim()] = cell.Address.ColumnNumber;

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            string GetCell(string col) => headers.TryGetValue(col, out int idx)
                ? row.Cell(idx).GetValue<string>()?.Trim() ?? string.Empty
                : string.Empty;

            var fullName = GetCell("FullName");
            var email = GetCell("Email");
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                continue;

            items.Add(new ImportStudentItem(fullName, email, GetCell("RollNumber"), GetCell("MemberCode")));
        }

        return items;
    }
}

public record AdminUpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Username,
    string? DisplayName,
    string? Password,
    string? RoleCode,
    bool? Status);

public record UpdateMyProfileRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Password);

public record AssignRoleRequest(string RoleCode);
