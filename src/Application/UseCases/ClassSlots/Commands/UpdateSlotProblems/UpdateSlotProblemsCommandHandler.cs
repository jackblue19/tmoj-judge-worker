using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class UpdateSlotProblemsCommandHandler : IRequestHandler<UpdateSlotProblemsCommand, int>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepo;
    private readonly IWriteRepository<ClassSlotProblem, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public UpdateSlotProblemsCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepo,
        IWriteRepository<ClassSlotProblem, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task<int> Handle(UpdateSlotProblemsCommand request, CancellationToken ct)
    {
        var slot = await _readRepo.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdWithProblemsSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        var slotProblems = slot.ClassSlotProblems?.ToList() ?? new List<ClassSlotProblem>();
        var updated = 0;

        foreach (var p in request.Problems)
        {
            var existing = slotProblems.FirstOrDefault(sp => sp.ProblemId == p.ProblemId);
            if (existing != null)
            {
                existing.Ordinal = p.Ordinal;
                existing.Points = p.Points;
                existing.IsRequired = p.IsRequired;
                _writeRepo.Update(existing);
                updated++;
            }
        }

        var (ok, error) = ValidateSlotPoints(slotProblems.Select(sp => sp.Points));
        if (!ok) throw new ArgumentException(error);

        if (updated > 0) await _uow.SaveChangesAsync(ct);

        return updated;
    }

    private const int MaxSlotPoints = 10;

    private static (bool Ok, string? Error) ValidateSlotPoints(IEnumerable<int?> allPoints)
    {
        var list = allPoints.ToList();
        if (list.Any(p => !p.HasValue))
            return (false, "Each problem must have Points.");
        var total = list.Sum(p => p!.Value);
        if (total > MaxSlotPoints)
            return (false, $"Total points exceeds {MaxSlotPoints}.");
        return (true, null);
    }
}
