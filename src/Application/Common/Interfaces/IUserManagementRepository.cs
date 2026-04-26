using Application.UseCases.Classes.Dtos;
using Application.UseCases.Users.Dtos;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IUserManagementRepository
{
    // Queries
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct);
    Task<UserDto?> GetUserDtoByIdAsync(Guid id, CancellationToken ct);
    Task<UserProfileDto?> GetUserProfileByIdAsync(Guid id, CancellationToken ct);
    Task<List<SimpleUserDto>> GetActiveUsersAsync(CancellationToken ct);
    Task<SimpleUserDto?> GetActiveUserByEmailAsync(string email, CancellationToken ct);
    Task<List<UserDto>> GetUsersByRoleAsync(string roleCode, CancellationToken ct);
    Task<List<UserProfileDto>> GetUsersByStatusAsync(bool status, CancellationToken ct);
    Task<StudentProfileWithClassesDto?> GetStudentDetailAsync(Guid id, Guid? semesterId, Guid? subjectId, CancellationToken ct);
    Task<TeacherDetailDto?> GetTeacherDetailAsync(Guid id, Guid? semesterId, Guid? subjectId, CancellationToken ct);

    // Entity helpers
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId, CancellationToken ct);
    Task<Guid?> GetRoleIdByCodeAsync(string roleCode, CancellationToken ct);
    Task<User?> FindUserAsync(Guid id, CancellationToken ct);
    Task<User?> FindUserByEmailAsync(string email, CancellationToken ct);
    void AddUser(User user);
    void RemoveUser(User user);
    Task SaveAsync(CancellationToken ct);
}
