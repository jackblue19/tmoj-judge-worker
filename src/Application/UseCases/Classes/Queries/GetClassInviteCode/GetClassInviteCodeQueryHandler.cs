using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassInviteCode;

public class GetClassInviteCodeQueryHandler : IRequestHandler<GetClassInviteCodeQuery, InviteCodeStatusDto>
{
    private readonly IClassRepository _repo;

    public GetClassInviteCodeQueryHandler(IClassRepository repo) => _repo = repo;

    public Task<InviteCodeStatusDto> Handle(GetClassInviteCodeQuery request, CancellationToken ct) =>
        _repo.GetInviteCodeStatusAsync(request.ClassSemesterId, ct);
}
