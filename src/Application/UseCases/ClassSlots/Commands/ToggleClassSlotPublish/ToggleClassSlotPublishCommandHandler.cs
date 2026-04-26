using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class ToggleClassSlotPublishCommandHandler : IRequestHandler<ToggleClassSlotPublishCommand, bool>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepo;
    private readonly IWriteRepository<ClassSlot, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public ToggleClassSlotPublishCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepo,
        IWriteRepository<ClassSlot, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(ToggleClassSlotPublishCommand request, CancellationToken ct)
    {
        var slot = await _readRepo.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        slot.IsPublished = !slot.IsPublished;
        _writeRepo.Update(slot);
        await _uow.SaveChangesAsync(ct);

        return slot.IsPublished;
    }
}
