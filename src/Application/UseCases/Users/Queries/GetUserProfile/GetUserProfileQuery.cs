using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto?>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IUserManagementRepository _repo;

    public GetUserProfileQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<UserProfileDto?> Handle(GetUserProfileQuery req, CancellationToken ct) =>
        _repo.GetUserProfileByIdAsync(req.UserId, ct);
}
