using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetUserByEmail;

public record GetUserByEmailQuery(string Email) : IRequest<SimpleUserDto?>;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, SimpleUserDto?>
{
    private readonly IUserManagementRepository _repo;

    public GetUserByEmailQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<SimpleUserDto?> Handle(GetUserByEmailQuery req, CancellationToken ct) =>
        _repo.GetActiveUserByEmailAsync(req.Email.Trim().ToLowerInvariant(), ct);
}
