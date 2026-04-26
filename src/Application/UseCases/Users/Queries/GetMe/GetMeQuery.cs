using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetMe;

public record GetMeQuery(Guid UserId) : IRequest<UserDto?>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto?>
{
    private readonly IUserManagementRepository _repo;

    public GetMeQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<UserDto?> Handle(GetMeQuery req, CancellationToken ct) =>
        _repo.GetUserDtoByIdAsync(req.UserId, ct);
}
