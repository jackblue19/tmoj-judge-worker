using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class DeleteClassSlotCommandHandler : IRequestHandler<DeleteClassSlotCommand>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepo;
    private readonly IWriteRepository<ClassSlot, Guid> _writeRepoSlot;
    private readonly IWriteRepository<ClassSlotProblem, Guid> _writeRepoSlotProblem;
    private readonly IUnitOfWork _uow;

    public DeleteClassSlotCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepo,
        IWriteRepository<ClassSlot, Guid> writeRepoSlot,
        IWriteRepository<ClassSlotProblem, Guid> writeRepoSlotProblem,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepoSlot = writeRepoSlot;
        _writeRepoSlotProblem = writeRepoSlotProblem;
        _uow = uow;
    }

    public async Task Handle(DeleteClassSlotCommand request, CancellationToken ct)
    {
        var slot = await _readRepo.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdWithProblemsSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        if (slot.ClassSlotProblems?.Any() == true)
        {
            _writeRepoSlotProblem.RemoveRange(slot.ClassSlotProblems);
        }

        _writeRepoSlot.Remove(slot);
        await _uow.SaveChangesAsync(ct);
    }
}
