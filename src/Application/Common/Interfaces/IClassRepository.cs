using Application.UseCases.Classes.Commands.CreateClassContest;
using Application.UseCases.Classes.Dtos;

namespace Application.Common.Interfaces;

public interface IClassRepository
{
    // ── Existence helpers ──────────────────────────────────
    Task<bool> SemesterExistsAsync(Guid semesterId, CancellationToken ct = default);
    Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken ct = default);
    Task<bool> ClassSemesterExistsAsync(Guid classSemesterId, CancellationToken ct = default);
    Task<bool> ClassExistsAsync(Guid classId, CancellationToken ct = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default);

    // ── Reads returning DTOs ───────────────────────────────
    Task<ClassListDto> GetClassesAsync(Guid? semesterId, Guid? subjectId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ClassDto> GetClassByIdAsync(Guid classId, CancellationToken ct = default);
    Task<ClassListDto> GetMyClassesAsync(Guid userId, string role, Guid? semesterId, Guid? subjectId, int page, int pageSize, CancellationToken ct = default);
    Task<List<ClassMemberDto>> GetClassStudentsAsync(Guid classSemesterId, CancellationToken ct = default);
    Task<List<ClassContestSummaryDto>> GetClassContestsAsync(Guid classSemesterId, CancellationToken ct = default);
    Task<ClassContestDto> GetClassContestByIdAsync(Guid classSemesterId, Guid contestId, Guid userId, CancellationToken ct = default);
    Task<InviteCodeStatusDto> GetInviteCodeStatusAsync(Guid classSemesterId, CancellationToken ct = default);
    Task<PagedAvailableStudentsDto> GetAvailableStudentsAsync(Guid classSemesterId, string? search, int page, int pageSize, CancellationToken ct = default);

    // ── Class writes ───────────────────────────────────────
    Task<(Guid ClassId, Guid InstanceId)> CreateClassAsync(string code, Guid subjectId, Guid semesterId, Guid? teacherId, CancellationToken ct = default);
    Task UpdateClassAsync(Guid classId, bool? isActive, CancellationToken ct = default);
    Task DeleteClassAsync(Guid classId, CancellationToken ct = default);

    // ── ClassSemester writes ───────────────────────────────
    Task<Guid> AddClassSemesterAsync(Guid classId, Guid semesterId, Guid subjectId, Guid? teacherId, CancellationToken ct = default);
    Task RemoveClassSemesterAsync(Guid classId, Guid classSemesterId, CancellationToken ct = default);
    Task UpdateClassSemesterAsync(Guid classId, Guid classSemesterId, Guid? newClassId, Guid? semesterId, Guid? subjectId, Guid? teacherId, CancellationToken ct = default);

    // ── Role / invite code ────────────────────────────────
    Task AssignTeacherRoleAsync(Guid userId, CancellationToken ct = default);
    Task<(Guid ClassId, Guid ClassSemesterId)> JoinByInviteCodeAsync(string inviteCode, Guid userId, CancellationToken ct = default);
    Task<InviteCodeDto> GenerateInviteCodeAsync(Guid classSemesterId, int minutesValid, CancellationToken ct = default);
    Task CancelInviteCodeAsync(Guid classSemesterId, CancellationToken ct = default);

    // ── Student management ─────────────────────────────────
    Task AddStudentManuallyAsync(Guid classSemesterId, string? rollNumber, string? memberCode, CancellationToken ct = default);
    Task UpdateStudentStatusAsync(Guid classSemesterId, Guid studentId, bool isActive, CancellationToken ct = default);
    Task RemoveStudentAsync(Guid classSemesterId, Guid studentId, CancellationToken ct = default);

    // ── Contest operations ────────────────────────────────
    Task ExtendContestTimeAsync(Guid classSemesterId, Guid contestId, DateTime newEndAt, CancellationToken ct = default);
    Task JoinContestAsync(Guid classSemesterId, Guid contestId, Guid userId, CancellationToken ct = default);
    Task FreezeContestAsync(Guid classSemesterId, Guid contestId, Guid userId, CancellationToken ct = default);
    Task UnfreezeContestAsync(Guid classSemesterId, Guid contestId, Guid userId, CancellationToken ct = default);

    Task<Guid> AddContestProblemAsync(Guid classSemesterId, Guid contestId, Guid createdBy,
        Guid problemId, string? alias, int? ordinal, int? points, int? maxScore,
        int? timeLimitMs, int? memoryLimitKb, CancellationToken ct = default);

    Task UpdateContestProblemAsync(Guid classSemesterId, Guid contestId, Guid contestProblemId,
        string? alias, int? ordinal, int? points, int? maxScore,
        int? timeLimitMs, int? memoryLimitKb, CancellationToken ct = default);

    Task RemoveContestProblemAsync(Guid classSemesterId, Guid contestId, Guid contestProblemId,
        CancellationToken ct = default);

    Task<ContestProblemDto> GetContestProblemByIdAsync(Guid classSemesterId, Guid contestId, Guid contestProblemId,
        CancellationToken ct = default);
}
