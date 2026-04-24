using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class AddSlotProblemsCommandHandler : IRequestHandler<AddSlotProblemsCommand, int>
{
    private readonly IReadRepository<ClassSlot, Guid> _readRepoSlot;
    private readonly IReadRepository<Problem, Guid> _readRepoProblem;
    private readonly IWriteRepository<ClassSlotProblem, Guid> _writeRepoSlotProblem;
    private readonly IUnitOfWork _uow;

    public AddSlotProblemsCommandHandler(
        IReadRepository<ClassSlot, Guid> readRepoSlot,
        IReadRepository<Problem, Guid> readRepoProblem,
        IWriteRepository<ClassSlotProblem, Guid> writeRepoSlotProblem,
        IUnitOfWork uow)
    {
        _readRepoSlot = readRepoSlot;
        _readRepoProblem = readRepoProblem;
        _writeRepoSlotProblem = writeRepoSlotProblem;
        _uow = uow;
    }

    public async Task<int> Handle(AddSlotProblemsCommand request, CancellationToken ct)
    {
        var slot = await _readRepoSlot.FirstOrDefaultAsync(
            new ClassSlotByClassSemesterAndIdWithProblemsSpec(request.ClassSemesterId, request.SlotId), ct)
            ?? throw new KeyNotFoundException("Slot not found.");

        var existing = (slot.ClassSlotProblems ?? new List<ClassSlotProblem>()).ToList();
        var existingProblemIds = existing.Select(sp => sp.ProblemId).ToHashSet();

        var newToAdd = request.Problems.Where(p => !existingProblemIds.Contains(p.ProblemId)).ToList();
        var combinedPoints = existing.Select(sp => sp.Points).Concat(newToAdd.Select(p => p.Points));
        var (ok, error) = ValidateSlotPoints(combinedPoints);
        if (!ok) throw new ArgumentException(error);

        var added = 0;
        foreach (var p in request.Problems)
        {
            if (existingProblemIds.Contains(p.ProblemId)) continue;

            if (!await _readRepoProblem.AnyAsync(new ProblemByIdSpec(p.ProblemId), ct))
                throw new ArgumentException($"Problem {p.ProblemId} not found.");

            var slotProblem = new ClassSlotProblem
            {
                SlotId = slot.Id,
                ProblemId = p.ProblemId,
                Ordinal = p.Ordinal,
                Points = p.Points,
                IsRequired = p.IsRequired
            };
            await _writeRepoSlotProblem.AddAsync(slotProblem, ct);
            added++;
        }

        await _uow.SaveChangesAsync(ct);
        return added;
    }

    private const int MaxSlotPoints = 10;

    private static (bool Ok, string? Error) ValidateSlotPoints(IEnumerable<int?> allPoints)
    {
        var list = allPoints.ToList();
        if (list.Any(p => !p.HasValue))
            return (false, "Each problem must have Points (not null).");
        if (list.Any(p => p!.Value < 0))
            return (false, "Points must be non-negative.");
        var total = list.Sum(p => p!.Value);
        if (total > MaxSlotPoints)
            return (false, $"Total points must not exceed {MaxSlotPoints}. Current: {total}.");
        return (true, null);
    }
}
