using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class RemoveSlotProblemsCommandHandler : IRequestHandler<RemoveSlotProblemsCommand, int>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepoSlot;
    private readonly IReadRepository<ClassSlotProblem, Guid> _readRepoSlotProblem;
    private readonly IWriteRepository<ClassSlotProblem, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public RemoveSlotProblemsCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepoSlot,
        IReadRepository<ClassSlotProblem, Guid> readRepoSlotProblem,
        IWriteRepository<ClassSlotProblem, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepoSlot = readRepoSlot;
        _readRepoSlotProblem = readRepoSlotProblem;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task<int> Handle(RemoveSlotProblemsCommand request, CancellationToken ct)
    {
        var slot = await _readRepoSlot.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        var toRemove = await _readRepoSlotProblem.ListAsync(
            new SlotProblemsBySlotAndProblemIdsSpec(request.SlotId, request.ProblemIds), ct);

        if (toRemove.Count == 0)
            throw new KeyNotFoundException("None of the specified problems were found.");

        _writeRepo.RemoveRange(toRemove);

        await _uow.SaveChangesAsync(ct);
        return toRemove.Count;
    }
}
