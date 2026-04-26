using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.ListAllUsers;

public record ListAllUsersQuery : IRequest<List<UserDto>>;

public class ListAllUsersQueryHandler : IRequestHandler<ListAllUsersQuery, List<UserDto>>
{
    private readonly IUserManagementRepository _repo;

    public ListAllUsersQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<List<UserDto>> Handle(ListAllUsersQuery req, CancellationToken ct) =>
        _repo.GetAllUsersAsync(ct);
}
