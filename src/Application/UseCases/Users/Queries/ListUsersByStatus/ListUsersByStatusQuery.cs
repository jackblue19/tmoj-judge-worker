using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.ListUsersByStatus;

public record ListUsersByStatusQuery(bool Status) : IRequest<List<UserProfileDto>>;

public class ListUsersByStatusQueryHandler : IRequestHandler<ListUsersByStatusQuery, List<UserProfileDto>>
{
    private readonly IUserManagementRepository _repo;

    public ListUsersByStatusQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<List<UserProfileDto>> Handle(ListUsersByStatusQuery req, CancellationToken ct) =>
        _repo.GetUsersByStatusAsync(req.Status, ct);
}
