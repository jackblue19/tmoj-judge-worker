using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetActiveUsers;

public record GetActiveUsersQuery : IRequest<List<SimpleUserDto>>;

public class GetActiveUsersQueryHandler : IRequestHandler<GetActiveUsersQuery, List<SimpleUserDto>>
{
    private readonly IUserManagementRepository _repo;

    public GetActiveUsersQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<List<SimpleUserDto>> Handle(GetActiveUsersQuery req, CancellationToken ct) =>
        _repo.GetActiveUsersAsync(ct);
}
