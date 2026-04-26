using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.ListUsersByRole;

public record ListUsersByRoleQuery(string RoleName) : IRequest<List<UserDto>>;

public class ListUsersByRoleQueryHandler : IRequestHandler<ListUsersByRoleQuery, List<UserDto>>
{
    private readonly IUserManagementRepository _repo;

    public ListUsersByRoleQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public async Task<List<UserDto>> Handle(ListUsersByRoleQuery req, CancellationToken ct)
    {
        var normalizedRole = req.RoleName.Trim().ToLowerInvariant();
        var roleId = await _repo.GetRoleIdByCodeAsync(normalizedRole, ct);
        if (roleId == null) throw new KeyNotFoundException("Role name not found.");
        return await _repo.GetUsersByRoleAsync(normalizedRole, ct);
    }
}
