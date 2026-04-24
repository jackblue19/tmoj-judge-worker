using Application.Common.Interfaces;
using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Commands.GenerateInviteCode;

public class GenerateInviteCodeCommandHandler : IRequestHandler<GenerateInviteCodeCommand, InviteCodeDto>
{
    private readonly IClassRepository _repo;

    public GenerateInviteCodeCommandHandler(IClassRepository repo) => _repo = repo;

    public Task<InviteCodeDto> Handle(GenerateInviteCodeCommand request, CancellationToken ct) =>
        _repo.GenerateInviteCodeAsync(request.ClassSemesterId, request.MinutesValid, ct);
}
