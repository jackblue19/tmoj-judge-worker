using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using Application.UseCases.Users.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserManagementRepository : IUserManagementRepository
{
    private readonly TmojDbContext _db;

    public UserManagementRepository(TmojDbContext db) => _db = db;

    public Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct) =>
        _db.Users
            .Include(u => u.Role)
            .Select(u => new UserDto(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName,
                u.Username, u.RollNumber, u.MemberCode, u.AvatarUrl, u.EmailVerified, u.Status,
                u.Role != null ? u.Role.RoleCode : null))
            .ToListAsync(ct);

    public Task<UserDto?> GetUserDtoByIdAsync(Guid id, CancellationToken ct) =>
        _db.Users
            .Where(u => u.UserId == id && u.DeletedAt == null)
            .Include(u => u.Role)
            .Select(u => new UserDto(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName,
                u.Username, u.RollNumber, u.MemberCode, u.AvatarUrl, u.EmailVerified, u.Status,
                u.Role != null ? u.Role.RoleCode : null))
            .FirstOrDefaultAsync(ct);

    public Task<UserProfileDto?> GetUserProfileByIdAsync(Guid id, CancellationToken ct) =>
        _db.Users
            .Where(u => u.UserId == id && u.DeletedAt == null)
            .Select(u => new UserProfileDto(u.UserId, u.Email, u.FirstName, u.LastName,
                u.DisplayName, u.Username, u.AvatarUrl, u.EmailVerified, u.Status, u.CreatedAt))
            .FirstOrDefaultAsync(ct);

    public Task<List<SimpleUserDto>> GetActiveUsersAsync(CancellationToken ct) =>
        _db.Users
            .AsNoTracking()
            .Where(u => u.DeletedAt == null && u.Status == true)
            .OrderBy(u => u.DisplayName)
            .Select(u => new SimpleUserDto(u.UserId, u.DisplayName, u.Email, u.AvatarUrl))
            .ToListAsync(ct);

    public Task<SimpleUserDto?> GetActiveUserByEmailAsync(string email, CancellationToken ct) =>
        _db.Users
            .AsNoTracking()
            .Where(u => u.Email == email && u.DeletedAt == null && u.Status == true)
            .Select(u => new SimpleUserDto(u.UserId, u.DisplayName, u.Email, u.AvatarUrl))
            .FirstOrDefaultAsync(ct);

    public async Task<List<UserDto>> GetUsersByRoleAsync(string roleCode, CancellationToken ct)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
        if (role == null) return [];

        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.RoleId == role.RoleId)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserDto(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName,
                u.Username, u.RollNumber, u.MemberCode, u.AvatarUrl, u.EmailVerified, u.Status,
                u.Role != null ? u.Role.RoleCode : null))
            .ToListAsync(ct);
    }

    public Task<List<UserProfileDto>> GetUsersByStatusAsync(bool status, CancellationToken ct) =>
        _db.Users
            .Where(u => u.Status == status)
            .Select(u => new UserProfileDto(u.UserId, u.Email, u.FirstName, u.LastName,
                u.DisplayName, u.Username, u.AvatarUrl, u.EmailVerified, u.Status, u.CreatedAt))
            .ToListAsync(ct);

    public async Task<StudentProfileWithClassesDto?> GetStudentDetailAsync(
        Guid id, Guid? semesterId, Guid? subjectId, CancellationToken ct)
    {
        var student = await _db.Users.AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.UserId == id && u.DeletedAt == null)
            .Select(u => new UserDto(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName,
                u.Username, u.RollNumber, u.MemberCode, u.AvatarUrl, u.EmailVerified, u.Status,
                u.Role != null ? u.Role.RoleCode : null))
            .FirstOrDefaultAsync(ct);

        if (student == null) return null;

        var classQuery = _db.ClassMembers.AsNoTracking().Where(m => m.UserId == id && m.IsActive);
        if (semesterId.HasValue) classQuery = classQuery.Where(m => m.ClassSemester.SemesterId == semesterId.Value);
        if (subjectId.HasValue) classQuery = classQuery.Where(m => m.ClassSemester.SubjectId == subjectId.Value);

        var classes = await classQuery
            .OrderByDescending(m => m.ClassSemester.CreatedAt)
            .Select(m => new ClassInstanceDto(
                m.ClassSemester.Id, m.ClassSemester.Class.ClassCode,
                m.ClassSemester.Semester.SemesterId, m.ClassSemester.Semester.Code,
                m.ClassSemester.Subject.SubjectId, m.ClassSemester.Subject.Code,
                m.ClassSemester.Subject.Name, m.ClassSemester.Subject.Description,
                m.ClassSemester.Semester.StartAt, m.ClassSemester.Semester.EndAt,
                (string?)null, (DateTime?)null, m.ClassSemester.CreatedAt,
                m.ClassSemester.Teacher != null
                    ? new ClassTeacherDto(m.ClassSemester.Teacher.UserId, m.ClassSemester.Teacher.DisplayName,
                        m.ClassSemester.Teacher.Email, m.ClassSemester.Teacher.AvatarUrl)
                    : null,
                m.ClassSemester.ClassMembers.Count(cm => cm.IsActive)))
            .ToListAsync(ct);

        return new StudentProfileWithClassesDto(student, classes, classes.Count);
    }

    public async Task<TeacherDetailDto?> GetTeacherDetailAsync(
        Guid id, Guid? semesterId, Guid? subjectId, CancellationToken ct)
    {
        var teacher = await _db.Users.AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.UserId == id && u.DeletedAt == null)
            .Select(u => new UserDto(u.UserId, u.Email, u.FirstName, u.LastName, u.DisplayName,
                u.Username, u.RollNumber, u.MemberCode, u.AvatarUrl, u.EmailVerified, u.Status,
                u.Role != null ? u.Role.RoleCode : null))
            .FirstOrDefaultAsync(ct);

        if (teacher == null) return null;

        var query = _db.ClassSemesters.AsNoTracking()
            .Include(cs => cs.Class).Include(cs => cs.Semester).Include(cs => cs.Subject)
            .Include(cs => cs.Teacher).Include(cs => cs.ClassMembers)
            .Where(cs => cs.TeacherId == id);

        if (semesterId.HasValue) query = query.Where(cs => cs.SemesterId == semesterId.Value);
        if (subjectId.HasValue) query = query.Where(cs => cs.SubjectId == subjectId.Value);

        var classSemesters = await query.ToListAsync(ct);

        var classes = classSemesters
            .Where(cs => cs.Semester != null && cs.Subject != null && cs.Class != null)
            .OrderByDescending(cs => cs.CreatedAt)
            .Select(cs => new ClassInstanceDto(
                cs.Id, cs.Class.ClassCode,
                cs.Semester.SemesterId, cs.Semester.Code,
                cs.Subject.SubjectId, cs.Subject.Code, cs.Subject.Name, cs.Subject.Description,
                cs.Semester.StartAt, cs.Semester.EndAt,
                null, null, cs.CreatedAt,
                cs.Teacher != null
                    ? new ClassTeacherDto(cs.Teacher.UserId, cs.Teacher.DisplayName, cs.Teacher.Email, cs.Teacher.AvatarUrl)
                    : null,
                cs.ClassMembers.Count(m => m.IsActive)))
            .ToList();

        var subjects = classSemesters
            .Where(cs => cs.Subject != null)
            .GroupBy(cs => cs.Subject.SubjectId)
            .Select(g => new TeacherSubjectInfoDto(
                g.Key, g.First().Subject.Code, g.First().Subject.Name,
                g.First().Subject.Description, g.Count()))
            .OrderBy(s => s.Code)
            .ToList();

        return new TeacherDetailDto(teacher, subjects, classes, classes.Count);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId, CancellationToken ct) =>
        _db.Users.AnyAsync(u => u.Username == username && (excludeUserId == null || u.UserId != excludeUserId), ct);

    public async Task<Guid?> GetRoleIdByCodeAsync(string roleCode, CancellationToken ct)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
        return role?.RoleId;
    }

    public Task<User?> FindUserAsync(Guid id, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.UserId == id && u.DeletedAt == null, ct);

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public void AddUser(User user) => _db.Users.Add(user);

    public void RemoveUser(User user) => _db.Users.Remove(user);

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
