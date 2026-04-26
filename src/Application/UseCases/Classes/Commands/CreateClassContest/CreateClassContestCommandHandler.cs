using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.CreateClassContest;

public class CreateClassContestCommandHandler : IRequestHandler<CreateClassContestCommand, (Guid ContestId, Guid SlotId)>
{
    private readonly IClassRepository _repo;

    public CreateClassContestCommandHandler(IClassRepository repo) => _repo = repo;

    public Task<(Guid ContestId, Guid SlotId)> Handle(CreateClassContestCommand request, CancellationToken ct) =>
        _repo.CreateContestAsync(
            request.ClassSemesterId, request.CreatedByUserId, request.Title, request.Slug,
            request.DescriptionMd, request.StartAt, request.EndAt, request.FreezeAt,
            request.Rules, request.Problems, request.SlotNo, request.SlotTitle, ct);
}
