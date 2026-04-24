using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class SetClassSlotDueDateCommandHandler : IRequestHandler<SetClassSlotDueDateCommand>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepo;
    private readonly IWriteRepository<ClassSlot, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public SetClassSlotDueDateCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepo,
        IWriteRepository<ClassSlot, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task Handle(SetClassSlotDueDateCommand request, CancellationToken ct)
    {
        var slot = await _readRepo.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        slot.DueAt = request.DueAt;
        if (request.CloseAt.HasValue) slot.CloseAt = request.CloseAt.Value;

        _writeRepo.Update(slot);
        await _uow.SaveChangesAsync(ct);
    }
}
