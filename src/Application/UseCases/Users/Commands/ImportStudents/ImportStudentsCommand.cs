using Application.Common.Interfaces;
using Application.UseCases.Auth.Hasher;
using Application.UseCases.Users.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Users.Commands.ImportStudents;

public record ImportStudentsCommand(List<ImportStudentItem> Students) : IRequest<ImportStudentsResult>;

public record ImportStudentsResult(int TotalProcessed, int SuccessCount, int FailedCount, List<string> Errors);

public class ImportStudentsCommandHandler : IRequestHandler<ImportStudentsCommand, ImportStudentsResult>
{
    private readonly IUserManagementRepository _repo;
    private readonly IPasswordHasher _hasher;

    public ImportStudentsCommandHandler(IUserManagementRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task<ImportStudentsResult> Handle(ImportStudentsCommand req, CancellationToken ct)
    {
        var studentRoleId = await _repo.GetRoleIdByCodeAsync("student", ct);
        int successCount = 0, failedCount = 0;
        var errors = new List<string>();
        int rowNumber = 2;

        foreach (var item in req.Students)
        {
            try
            {
                var email = item.Email.Trim().ToLowerInvariant();
                var existing = await _repo.FindUserByEmailAsync(email, ct);

                if (existing == null)
                {
                    var parts = SplitFullName(item.FullName);
                    var defaultPassword = !string.IsNullOrWhiteSpace(item.MemberCode) ? item.MemberCode.Trim()
                        : !string.IsNullOrWhiteSpace(item.RollNumber) ? item.RollNumber.Trim()
                        : email.Split('@')[0];

                    var user = new User
                    {
                        FirstName = parts.FirstName,
                        LastName = parts.LastName,
                        Email = email,
                        Password = _hasher.Hash(defaultPassword),
                        Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N")[..4],
                        DisplayName = item.FullName,
                        RollNumber = item.RollNumber,
                        MemberCode = item.MemberCode,
                        Status = true,
                        EmailVerified = true,
                        LanguagePreference = "en",
                        RoleId = studentRoleId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _repo.AddUser(user);
                    await _repo.SaveAsync(ct);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNumber}: {ex.Message}");
                failedCount++;
            }
            finally
            {
                rowNumber++;
            }
        }

        return new ImportStudentsResult(req.Students.Count, successCount, failedCount, errors);
    }

    private static (string LastName, string FirstName) SplitFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return ("Unknown", "Unknown");
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return (parts[0], parts[0]);
        return (parts[0], string.Join(" ", parts.Skip(1)));
    }
}
