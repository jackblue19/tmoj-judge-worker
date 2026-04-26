using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class UpdateClassSlotCommandHandler : IRequestHandler<UpdateClassSlotCommand>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepo;
    private readonly IWriteRepository<ClassSlot, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public UpdateClassSlotCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepo,
        IWriteRepository<ClassSlot, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task Handle(UpdateClassSlotCommand request, CancellationToken ct)
    {
        var slot = await _readRepo.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        if (request.Title is not null) slot.Title = request.Title.Trim();
        if (request.Description is not null) slot.Description = request.Description.Trim();
        if (request.Rules is not null) slot.Rules = request.Rules.Trim();
        if (request.OpenAt.HasValue) slot.OpenAt = request.OpenAt.Value.ToUniversalTime();
        if (request.DueAt.HasValue) slot.DueAt = request.DueAt.Value.ToUniversalTime();
        if (request.CloseAt.HasValue) slot.CloseAt = request.CloseAt.Value.ToUniversalTime();
        if (request.IsPublished.HasValue) slot.IsPublished = request.IsPublished.Value;

        _writeRepo.Update(slot);
        await _uow.SaveChangesAsync(ct);
    }
}
