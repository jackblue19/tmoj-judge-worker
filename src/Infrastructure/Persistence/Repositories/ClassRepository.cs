using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ClassRepository : IClassRepository
{
    private readonly TmojDbContext _db;

    public ClassRepository(TmojDbContext db) => _db = db;

    // ── Existence helpers ──────────────────────────────────

    public Task<bool> SemesterExistsAsync(Guid semesterId, CancellationToken ct = default) =>
        _db.Semesters.AnyAsync(s => s.SemesterId == semesterId, ct);

    public Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken ct = default) =>
        _db.Subjects.AnyAsync(s => s.SubjectId == subjectId, ct);

    public Task<bool> ClassSemesterExistsAsync(Guid classSemesterId, CancellationToken ct = default) =>
        _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct);

    public Task<bool> ClassExistsAsync(Guid classId, CancellationToken ct = default) =>
        _db.Classes.AnyAsync(c => c.ClassId == classId, ct);

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.UserId == userId, ct);

    // ── Reads returning DTOs ───────────────────────────────

    public async Task<ClassListDto> GetClassesAsync(Guid? semesterId, Guid? subjectId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Classes.AsNoTracking()
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Subject)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Teacher)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
            .Where(c => c.IsActive);

        if (semesterId.HasValue)
            query = query.Where(c => c.ClassSemesters.Any(cs => cs.SemesterId == semesterId.Value));
        if (subjectId.HasValue)
            query = query.Where(c => c.ClassSemesters.Any(cs => cs.SubjectId == subjectId.Value));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.ClassCode.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var result = items.Select(c =>
        {
            var filtered = c.ClassSemesters.Where(cs => cs.Semester != null && cs.Subject != null);
            if (semesterId.HasValue) filtered = filtered.Where(cs => cs.SemesterId == semesterId.Value);
            if (subjectId.HasValue) filtered = filtered.Where(cs => cs.SubjectId == subjectId.Value);
            return MapToClassDto(c, filtered.ToList());
        }).ToList();

        return new ClassListDto(result, totalCount);
    }

    public async Task<ClassDto> GetClassByIdAsync(Guid classId, CancellationToken ct = default)
    {
        var c = await _db.Classes.AsNoTracking()
            .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Semester)
            .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Subject)
            .Include(x => x.ClassSemesters).ThenInclude(cs => cs.Teacher)
            .Include(x => x.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
            .FirstOrDefaultAsync(x => x.ClassId == classId, ct)
            ?? throw new KeyNotFoundException("Class not found.");

        var instanceList = c.ClassSemesters.Where(cs => cs.Semester != null && cs.Subject != null).ToList();
        return MapToClassDto(c, instanceList);
    }

    public async Task<ClassListDto> GetMyClassesAsync(Guid userId, string role, Guid? semesterId, Guid? subjectId, int page, int pageSize, CancellationToken ct = default)
    {
        if (role == "teacher")
            return await GetMyClassesAsTeacherAsync(userId, semesterId, subjectId, page, pageSize, ct);
        return await GetMyClassesAsStudentAsync(userId, semesterId, subjectId, page, pageSize, ct);
    }

    private async Task<ClassListDto> GetMyClassesAsStudentAsync(Guid userId, Guid? semesterId, Guid? subjectId, int page, int pageSize, CancellationToken ct)
    {
        var memberCsIds = await _db.ClassMembers.AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => m.ClassSemesterId)
            .ToListAsync(ct);

        var query = _db.Classes.AsNoTracking()
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Subject)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Teacher)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
            .Where(c => c.IsActive && c.ClassSemesters.Any(cs => memberCsIds.Contains(cs.Id)));

        if (semesterId.HasValue)
            query = query.Where(c => c.ClassSemesters.Any(cs => cs.SemesterId == semesterId.Value && memberCsIds.Contains(cs.Id)));
        if (subjectId.HasValue)
            query = query.Where(c => c.ClassSemesters.Any(cs => cs.SubjectId == subjectId.Value && memberCsIds.Contains(cs.Id)));

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var result = items.Select(c =>
        {
            var filtered = c.ClassSemesters
                .Where(cs => cs.Semester != null && cs.Subject != null && memberCsIds.Contains(cs.Id));
            if (semesterId.HasValue) filtered = filtered.Where(cs => cs.SemesterId == semesterId.Value);
            if (subjectId.HasValue) filtered = filtered.Where(cs => cs.SubjectId == subjectId.Value);

            var instanceList = filtered.Select(cs => new ClassInstanceDto(
                cs.Id, c.ClassCode, cs.Semester.SemesterId, cs.Semester.Code,
                cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                cs.Semester.StartAt, cs.Semester.EndAt, null, null, cs.CreatedAt,
                cs.Teacher != null ? new ClassTeacherDto(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl) : null,
                cs.ClassMembers.Count(m => m.IsActive))).ToList();

            return new ClassDto(c.ClassId, c.ClassCode, c.IsActive, c.CreatedAt, c.UpdatedAt, instanceList, instanceList.Count);
        }).ToList();

        return new ClassListDto(result, totalCount);
    }

    private async Task<ClassListDto> GetMyClassesAsTeacherAsync(Guid userId, Guid? semesterId, Guid? subjectId, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Classes.AsNoTracking()
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Semester)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Subject)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.Teacher)
            .Include(c => c.ClassSemesters).ThenInclude(cs => cs.ClassMembers)
            .Where(c => c.IsActive && c.ClassSemesters.Any(cs => cs.TeacherId == userId));

        if (semesterId.HasValue)
            query = query.Where(c => c.ClassSemesters.Any(cs => cs.SemesterId == semesterId.Value && cs.TeacherId == userId));
        if (subjectId.HasValue)
            query = query.Where(c => c.ClassSemesters.Any(cs => cs.SubjectId == subjectId.Value && cs.TeacherId == userId));

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var result = items.Select(c =>
        {
            var filtered = c.ClassSemesters
                .Where(cs => cs.Semester != null && cs.Subject != null && cs.TeacherId == userId);
            if (semesterId.HasValue) filtered = filtered.Where(cs => cs.SemesterId == semesterId.Value);
            if (subjectId.HasValue) filtered = filtered.Where(cs => cs.SubjectId == subjectId.Value);
            return MapToClassDto(c, filtered.ToList());
        }).ToList();

        return new ClassListDto(result, totalCount);
    }

    public Task<List<ClassMemberDto>> GetClassStudentsAsync(Guid classSemesterId, CancellationToken ct = default) =>
        _db.ClassMembers.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.ClassSemesterId == classSemesterId)
            .OrderBy(m => m.User.DisplayName)
            .Select(m => new ClassMemberDto(
                m.Id, m.ClassSemesterId, m.UserId,
                m.User.DisplayName, m.User.Email, m.User.AvatarUrl,
                m.JoinedAt, m.IsActive))
            .ToListAsync(ct);

    public async Task<List<ClassContestSummaryDto>> GetClassContestsAsync(Guid classSemesterId, CancellationToken ct = default)
    {
        if (!await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct))
            throw new KeyNotFoundException("Class instance not found.");

        var contestSlots = await _db.ClassSlots.AsNoTracking()
            .Include(s => s.Contest).ThenInclude(c => c!.ContestProblems)
            .Include(s => s.Contest).ThenInclude(c => c!.ContestTeams)
            .Where(s => s.ClassSemesterId == classSemesterId && s.Mode == "contest" && s.ContestId != null)
            .ToListAsync(ct);

        return contestSlots
            .Where(s => s.Contest != null)
            .Select(s => new ClassContestSummaryDto(
                s.Contest!.Id, s.Contest.Title, s.Contest.Slug,
                s.Contest.StartAt, s.Contest.EndAt, s.Contest.IsActive,
                s.Contest.ContestProblems.Count, s.Contest.ContestTeams.Count))
            .ToList();
    }

    public async Task<ClassContestDto> GetClassContestByIdAsync(Guid classSemesterId, Guid contestId, Guid userId, CancellationToken ct = default)
    {
        var slot = await _db.ClassSlots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct)
            ?? throw new KeyNotFoundException("Contest not found in this class instance.");

        var contest = await _db.Contests.AsNoTracking()
            .Include(c => c.ContestProblems).ThenInclude(cp => cp.Problem)
            .Include(c => c.ContestTeams).ThenInclude(ct2 => ct2.Team)
            .FirstOrDefaultAsync(c => c.Id == contestId, ct)
            ?? throw new KeyNotFoundException("Contest not found.");

        var isJoined = contest.ContestTeams.Any(ct2 => ct2.Team.LeaderId == userId);
        if (!isJoined)
        {
            var userTeamIds = await _db.TeamMembers.AsNoTracking()
                .Where(tm => tm.UserId == userId)
                .Select(tm => tm.TeamId)
                .ToListAsync(ct);
            isJoined = contest.ContestTeams.Any(ct2 => userTeamIds.Contains(ct2.TeamId));
        }

        var now = DateTime.UtcNow;
        double? remaining = contest.EndAt > now ? (contest.EndAt - now).TotalSeconds : 0;

        return new ClassContestDto(
            contest.Id, classSemesterId, slot.Id,
            contest.Title, contest.Slug, contest.DescriptionMd, contest.Rules,
            contest.StartAt, contest.EndAt, contest.FreezeAt,
            contest.IsActive, isJoined, remaining, contest.CreatedAt,
            contest.ContestProblems.OrderBy(cp => cp.Ordinal).Select(cp => new ContestProblemDto(
                cp.Id, cp.ProblemId, cp.Problem.Title, cp.Problem.Slug,
                cp.Alias, cp.Ordinal, cp.Points, cp.MaxScore,
                cp.TimeLimitMs, cp.MemoryLimitKb)).ToList());
    }

    public async Task<InviteCodeStatusDto> GetInviteCodeStatusAsync(Guid classSemesterId, CancellationToken ct = default)
    {
        var instance = await _db.ClassSemesters.AsNoTracking()
            .Include(cs => cs.Class)
            .Include(cs => cs.Semester)
            .Include(cs => cs.Subject)
            .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct)
            ?? throw new KeyNotFoundException("Class instance not found.");

        var isActive = !string.IsNullOrEmpty(instance.InviteCode) &&
                       !(instance.InviteCodeExpiresAt.HasValue && instance.InviteCodeExpiresAt.Value < DateTime.UtcNow);

        double? remaining = null;
        if (instance.InviteCodeExpiresAt.HasValue)
            remaining = Math.Max(0, (instance.InviteCodeExpiresAt.Value - DateTime.UtcNow).TotalSeconds);

        return new InviteCodeStatusDto(
            instance.Id,
            instance.Class?.ClassCode,
            instance.Semester?.Code,
            instance.Subject?.Code,
            string.IsNullOrEmpty(instance.InviteCode) ? null : instance.InviteCode,
            instance.InviteCodeExpiresAt,
            isActive,
            remaining);
    }

    public async Task<PagedAvailableStudentsDto> GetAvailableStudentsAsync(Guid classSemesterId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var enrolledIds = _db.ClassMembers.AsNoTracking()
            .Where(m => m.ClassSemesterId == classSemesterId && m.IsActive)
            .Select(m => m.UserId);

        var studentRoleId = await _db.Roles.AsNoTracking()
            .Where(r => r.RoleCode == "student")
            .Select(r => (Guid?)r.RoleId)
            .FirstOrDefaultAsync(ct);

        var query = _db.Users.AsNoTracking()
            .Where(u => u.DeletedAt == null && u.Status)
            .Where(u => u.RoleId == studentRoleId)
            .Where(u => !enrolledIds.Contains(u.UserId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(s)) ||
                u.Email.ToLower().Contains(s) ||
                (u.RollNumber != null && u.RollNumber.ToLower().Contains(s)) ||
                (u.MemberCode != null && u.MemberCode.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AvailableStudentDto(
                u.UserId, u.Email, u.FirstName, u.LastName,
                u.DisplayName, u.RollNumber, u.MemberCode, u.AvatarUrl))
            .ToListAsync(ct);

        return new PagedAvailableStudentsDto(items, totalCount);
    }

    // ── Class writes ───────────────────────────────────────

    public async Task<(Guid ClassId, Guid InstanceId)> CreateClassAsync(string code, Guid subjectId, Guid semesterId, Guid? teacherId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("ClassCode is required.");

        if (!await _db.Subjects.AnyAsync(s => s.SubjectId == subjectId, ct))
            throw new KeyNotFoundException("Subject not found.");

        var codeNorm = code.Trim().ToUpperInvariant();
        var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassCode == codeNorm, ct);
        if (cls is null)
        {
            cls = new Class { ClassCode = codeNorm, IsActive = true };
            _db.Classes.Add(cls);
            await _db.SaveChangesAsync(ct);
        }
        else if (!cls.IsActive)
        {
            cls.IsActive = true;
            await _db.SaveChangesAsync(ct);
        }

        var exists = await _db.ClassSemesters.AnyAsync(cs =>
            cs.ClassId == cls.ClassId && cs.SemesterId == semesterId && cs.SubjectId == subjectId, ct);
        if (exists)
            throw new InvalidOperationException("This class is already enrolled in this subject and semester.");

        var instance = new ClassSemester
        {
            ClassId = cls.ClassId,
            SemesterId = semesterId,
            SubjectId = subjectId,
            TeacherId = teacherId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ClassSemesters.Add(instance);
        await _db.SaveChangesAsync(ct);

        return (cls.ClassId, instance.Id);
    }

    public async Task UpdateClassAsync(Guid classId, bool? isActive, CancellationToken ct = default)
    {
        var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct)
            ?? throw new KeyNotFoundException("Class not found.");

        if (isActive.HasValue)
            cls.IsActive = isActive.Value;

        cls.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteClassAsync(Guid classId, CancellationToken ct = default)
    {
        var cls = await _db.Classes.FirstOrDefaultAsync(c => c.ClassId == classId, ct)
            ?? throw new KeyNotFoundException("Class not found.");

        cls.IsActive = false;
        cls.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ── ClassSemester writes ───────────────────────────────

    public async Task<Guid> AddClassSemesterAsync(Guid classId, Guid semesterId, Guid subjectId, Guid? teacherId, CancellationToken ct = default)
    {
        if (!await _db.Classes.AnyAsync(c => c.ClassId == classId, ct))
            throw new KeyNotFoundException("Class not found.");

        if (!await _db.Semesters.AnyAsync(s => s.SemesterId == semesterId, ct))
            throw new KeyNotFoundException("Semester not found.");

        var exists = await _db.ClassSemesters.AnyAsync(cs =>
            cs.ClassId == classId && cs.SemesterId == semesterId && cs.SubjectId == subjectId, ct);
        if (exists)
            throw new InvalidOperationException("Class is already linked to this semester and subject.");

        var link = new ClassSemester
        {
            ClassId = classId,
            SemesterId = semesterId,
            SubjectId = subjectId,
            TeacherId = teacherId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ClassSemesters.Add(link);
        await _db.SaveChangesAsync(ct);

        return link.Id;
    }

    public async Task RemoveClassSemesterAsync(Guid classId, Guid classSemesterId, CancellationToken ct = default)
    {
        var link = await _db.ClassSemesters
            .Include(cs => cs.ClassMembers)
            .Include(cs => cs.ClassSlots)
            .FirstOrDefaultAsync(cs => cs.Id == classSemesterId && cs.ClassId == classId, ct)
            ?? throw new KeyNotFoundException("Class-Semester instance not found.");

        _db.ClassMembers.RemoveRange(link.ClassMembers);
        _db.ClassSlots.RemoveRange(link.ClassSlots);
        _db.ClassSemesters.Remove(link);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateClassSemesterAsync(Guid classId, Guid classSemesterId, Guid? newClassId, Guid? semesterId, Guid? subjectId, Guid? teacherId, CancellationToken ct = default)
    {
        var link = await _db.ClassSemesters
            .FirstOrDefaultAsync(cs => cs.Id == classSemesterId && cs.ClassId == classId, ct)
            ?? throw new KeyNotFoundException("Class-Semester instance not found.");

        if (newClassId.HasValue)
        {
            if (!await _db.Classes.AnyAsync(c => c.ClassId == newClassId.Value, ct))
                throw new KeyNotFoundException("Class not found.");
            link.ClassId = newClassId.Value;
        }

        if (semesterId.HasValue)
        {
            if (!await _db.Semesters.AnyAsync(s => s.SemesterId == semesterId.Value, ct))
                throw new KeyNotFoundException("Semester not found.");
            link.SemesterId = semesterId.Value;
        }

        if (subjectId.HasValue)
        {
            if (!await _db.Subjects.AnyAsync(s => s.SubjectId == subjectId.Value, ct))
                throw new KeyNotFoundException("Subject not found.");
            link.SubjectId = subjectId.Value;
        }

        if (teacherId.HasValue)
        {
            if (teacherId.Value == Guid.Empty)
            {
                link.TeacherId = null;
            }
            else
            {
                if (!await _db.Users.AnyAsync(u => u.UserId == teacherId.Value, ct))
                    throw new KeyNotFoundException("Teacher user not found.");
                link.TeacherId = teacherId.Value;
            }
            _db.Entry(link).Property(x => x.TeacherId).IsModified = true;
        }

        var duplicate = await _db.ClassSemesters.AnyAsync(cs =>
            cs.Id != classSemesterId &&
            cs.ClassId == link.ClassId &&
            cs.SemesterId == link.SemesterId &&
            cs.SubjectId == link.SubjectId, ct);
        if (duplicate)
            throw new InvalidOperationException("A class-semester instance with this combination already exists.");

        _db.Entry(link).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
    }

    // ── Role / invite code ─────────────────────────────────

    public async Task AssignTeacherRoleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var teacherRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "teacher", ct)
            ?? throw new InvalidOperationException("Teacher role not found in system.");

        if (user.RoleId == teacherRole.RoleId)
            throw new InvalidOperationException("User already has the teacher role.");

        user.RoleId = teacherRole.RoleId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(Guid ClassId, Guid ClassSemesterId)> JoinByInviteCodeAsync(string inviteCode, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ArgumentException("Invite code is required.");

        var instance = await _db.ClassSemesters
            .Include(cs => cs.Class)
            .FirstOrDefaultAsync(cs => cs.InviteCode == inviteCode.Trim(), ct)
            ?? throw new KeyNotFoundException("Invalid or expired invite code.");

        if (instance.InviteCodeExpiresAt.HasValue && instance.InviteCodeExpiresAt.Value < DateTime.UtcNow)
        {
            instance.InviteCode = null;
            instance.InviteCodeExpiresAt = null;
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Invite code has expired.");
        }

        var already = await _db.ClassMembers.AnyAsync(
            m => m.ClassSemesterId == instance.Id && m.UserId == userId, ct);

        if (!already)
        {
            _db.ClassMembers.Add(new ClassMember
            {
                ClassSemesterId = instance.Id,
                UserId = userId,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        return (instance.ClassId, instance.Id);
    }

    public async Task<InviteCodeDto> GenerateInviteCodeAsync(Guid classSemesterId, int minutesValid, CancellationToken ct = default)
    {
        var instance = await _db.ClassSemesters
            .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct)
            ?? throw new KeyNotFoundException("Class instance not found.");

        int minutes = minutesValid <= 0 ? 15 : Math.Min(minutesValid, 15);

        instance.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
        instance.InviteCodeExpiresAt = DateTime.UtcNow.AddMinutes(minutes);

        await _db.SaveChangesAsync(ct);

        return new InviteCodeDto(instance.Id, instance.InviteCode, instance.InviteCodeExpiresAt.Value);
    }

    public async Task CancelInviteCodeAsync(Guid classSemesterId, CancellationToken ct = default)
    {
        var instance = await _db.ClassSemesters
            .FirstOrDefaultAsync(cs => cs.Id == classSemesterId, ct)
            ?? throw new KeyNotFoundException("Class instance not found.");

        instance.InviteCode = null;
        instance.InviteCodeExpiresAt = null;

        await _db.SaveChangesAsync(ct);
    }

    // ── Student management ─────────────────────────────────

    public async Task AddStudentManuallyAsync(Guid classSemesterId, string? rollNumber, string? memberCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rollNumber) && string.IsNullOrWhiteSpace(memberCode))
            throw new ArgumentException("Must provide either RollNumber or MemberCode.");

        if (!await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct))
            throw new KeyNotFoundException("Class instance not found.");

        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(rollNumber))
            query = query.Where(u => u.RollNumber == rollNumber.Trim());
        else
            query = query.Where(u => u.MemberCode == memberCode!.Trim());

        var student = await query.FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Student not found in the system.");

        var existing = await _db.ClassMembers.FirstOrDefaultAsync(
            m => m.ClassSemesterId == classSemesterId && m.UserId == student.UserId, ct);

        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await _db.SaveChangesAsync(ct);
                return;
            }
            throw new InvalidOperationException("Student is already an active member of this class.");
        }

        _db.ClassMembers.Add(new ClassMember
        {
            ClassSemesterId = classSemesterId,
            UserId = student.UserId,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStudentStatusAsync(Guid classSemesterId, Guid studentId, bool isActive, CancellationToken ct = default)
    {
        if (!await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct))
            throw new KeyNotFoundException("Class instance not found.");

        var member = await _db.ClassMembers.FirstOrDefaultAsync(
            m => m.ClassSemesterId == classSemesterId && m.UserId == studentId, ct)
            ?? throw new KeyNotFoundException("Student is not in this class.");

        member.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveStudentAsync(Guid classSemesterId, Guid studentId, CancellationToken ct = default)
    {
        if (!await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct))
            throw new KeyNotFoundException("Class instance not found.");

        var member = await _db.ClassMembers.FirstOrDefaultAsync(
            m => m.ClassSemesterId == classSemesterId && m.UserId == studentId, ct)
            ?? throw new KeyNotFoundException("Student is not in this class.");

        _db.ClassMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
    }

    // ── Contest operations ─────────────────────────────────

    public async Task ExtendContestTimeAsync(Guid classSemesterId, Guid contestId, DateTime newEndAt, CancellationToken ct = default)
    {
        if (!await _db.ClassSemesters.AnyAsync(cs => cs.Id == classSemesterId, ct))
            throw new KeyNotFoundException("Class instance not found.");

        var slotExists = await _db.ClassSlots.AnyAsync(
            s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
        if (!slotExists)
            throw new KeyNotFoundException("Contest not found in this class instance.");

        var contest = await _db.Contests.FirstOrDefaultAsync(c => c.Id == contestId, ct)
            ?? throw new KeyNotFoundException("Contest not found.");

        if (newEndAt <= contest.EndAt)
            throw new ArgumentException("New end time must be after current end time.");

        contest.EndAt = DateTime.SpecifyKind(newEndAt, DateTimeKind.Utc);
        await _db.SaveChangesAsync(ct);
    }

    public async Task JoinContestAsync(Guid classSemesterId, Guid contestId, Guid userId, CancellationToken ct = default)
    {
        var isMember = await _db.ClassMembers.AnyAsync(
            m => m.ClassSemesterId == classSemesterId && m.UserId == userId && m.IsActive, ct);
        if (!isMember)
            throw new UnauthorizedAccessException("User is not an active member of this class.");

        var slotExists = await _db.ClassSlots.AnyAsync(
            s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
        if (!slotExists)
            throw new KeyNotFoundException("Contest not found in this class instance.");

        var contest = await _db.Contests.FirstOrDefaultAsync(c => c.Id == contestId && c.IsActive, ct)
            ?? throw new KeyNotFoundException("Contest not found or inactive.");

        var now = DateTime.UtcNow;
        if (now < contest.StartAt)
            throw new InvalidOperationException("Contest has not started yet.");
        if (now >= contest.EndAt)
            throw new InvalidOperationException("Contest has already ended.");

        var personalTeam = await _db.Teams
            .FirstOrDefaultAsync(t => t.LeaderId == userId && t.IsPersonal, ct);

        if (personalTeam is null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
            personalTeam = new Team
            {
                LeaderId = userId,
                TeamSize = 1,
                TeamName = user?.DisplayName ?? "Personal Team",
                IsPersonal = true
            };
            _db.Teams.Add(personalTeam);
            _db.TeamMembers.Add(new TeamMember
            {
                TeamId = personalTeam.Id,
                UserId = userId
            });
        }

        var alreadyJoined = await _db.ContestTeams.AnyAsync(
            ct2 => ct2.ContestId == contestId && ct2.TeamId == personalTeam.Id, ct);
        if (alreadyJoined)
            throw new InvalidOperationException("You have already joined this contest.");

        _db.ContestTeams.Add(new ContestTeam
        {
            ContestId = contestId,
            TeamId = personalTeam.Id
        });
        await _db.SaveChangesAsync(ct);
    }

    // ── Contest problem management ────────────────────────

    public async Task<Guid> AddContestProblemAsync(
        Guid classSemesterId, Guid contestId, Guid createdBy,
        Guid problemId, string? alias, int? ordinal, int? points, int? maxScore,
        int? timeLimitMs, int? memoryLimitKb, CancellationToken ct = default)
    {
        var slotExists = await _db.ClassSlots.AnyAsync(
            s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
        if (!slotExists)
            throw new KeyNotFoundException("Contest not found in this class.");

        var problem = await _db.Problems.FirstOrDefaultAsync(p => p.Id == problemId, ct)
            ?? throw new KeyNotFoundException($"Problem {problemId} not found.");

        var existing = await _db.ContestProblems
            .Where(cp => cp.ContestId == contestId && cp.IsActive)
            .ToListAsync(ct);

        if (existing.Any(cp => cp.ProblemId == problemId))
            throw new InvalidOperationException("Problem already exists in this contest.");

        var autoAlias = alias ?? ((char)('A' + existing.Count)).ToString();

        var entity = new ContestProblem
        {
            Id = Guid.NewGuid(),
            ContestId = contestId,
            ProblemId = problemId,
            Alias = autoAlias,
            Ordinal = ordinal ?? (existing.Count + 1),
            Points = points ?? 100,
            MaxScore = maxScore ?? 100,
            TimeLimitMs = timeLimitMs ?? problem.TimeLimitMs,
            MemoryLimitKb = memoryLimitKb ?? problem.MemoryLimitKb,
            IsActive = true,
            CreatedBy = createdBy
        };
        _db.ContestProblems.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task UpdateContestProblemAsync(
        Guid classSemesterId, Guid contestId, Guid contestProblemId,
        string? alias, int? ordinal, int? points, int? maxScore,
        int? timeLimitMs, int? memoryLimitKb, CancellationToken ct = default)
    {
        var slotExists = await _db.ClassSlots.AnyAsync(
            s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
        if (!slotExists)
            throw new KeyNotFoundException("Contest not found in this class.");

        var cp = await _db.ContestProblems
            .FirstOrDefaultAsync(p => p.Id == contestProblemId && p.ContestId == contestId, ct)
            ?? throw new KeyNotFoundException("Contest problem not found.");

        if (alias is not null) cp.Alias = alias;
        if (ordinal.HasValue) cp.Ordinal = ordinal;
        if (points.HasValue) cp.Points = points;
        if (maxScore.HasValue) cp.MaxScore = maxScore;
        if (timeLimitMs.HasValue) cp.TimeLimitMs = timeLimitMs;
        if (memoryLimitKb.HasValue) cp.MemoryLimitKb = memoryLimitKb;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveContestProblemAsync(
        Guid classSemesterId, Guid contestId, Guid contestProblemId, CancellationToken ct = default)
    {
        var slotExists = await _db.ClassSlots.AnyAsync(
            s => s.ClassSemesterId == classSemesterId && s.ContestId == contestId, ct);
        if (!slotExists)
            throw new KeyNotFoundException("Contest not found in this class.");

        var cp = await _db.ContestProblems
            .FirstOrDefaultAsync(p => p.Id == contestProblemId && p.ContestId == contestId, ct)
            ?? throw new KeyNotFoundException("Contest problem not found.");

        _db.ContestProblems.Remove(cp);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ────────────────────────────────────

    private static ClassDto MapToClassDto(Class c, List<ClassSemester> instanceList)
    {
        var instances = instanceList.Select(cs => new ClassInstanceDto(
            cs.Id, c.ClassCode,
            cs.Semester.SemesterId, cs.Semester.Code,
            cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
            cs.Semester.StartAt, cs.Semester.EndAt,
            cs.InviteCode, cs.InviteCodeExpiresAt, cs.CreatedAt,
            cs.Teacher != null ? new ClassTeacherDto(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl) : null,
            cs.ClassMembers.Count(m => m.IsActive))).ToList();

        return new ClassDto(
            c.ClassId, c.ClassCode, c.IsActive, c.CreatedAt, c.UpdatedAt, instances,
            instanceList.SelectMany(cs => cs.ClassMembers).Where(m => m.IsActive).Select(m => m.UserId).Distinct().Count());
    }
}
