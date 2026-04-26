using Application.Common.Interfaces;
using Application.UseCases.ClassSlots.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public class CreateClassSlotCommandHandler : IRequestHandler<CreateClassSlotCommand, Guid>
{
    private readonly IWriteRepository<ClassSlot, Guid> _writeRepoSlot;
    private readonly IWriteRepository<ClassSlotProblem, Guid> _writeRepoSlotProblem;
    private readonly IReadRepository<ClassSlot, Guid> _readRepoSlot;
    private readonly IReadRepository<Problem, Guid> _readRepoProblem;
    private readonly IUnitOfWork _uow;
    private readonly IClassSlotRepository _classSlotRepo;

    public CreateClassSlotCommandHandler(
        IWriteRepository<ClassSlot, Guid> writeRepoSlot,
        IWriteRepository<ClassSlotProblem, Guid> writeRepoSlotProblem,
        IReadRepository<ClassSlot, Guid> readRepoSlot,
        IReadRepository<Problem, Guid> readRepoProblem,
        IUnitOfWork uow,
        IClassSlotRepository classSlotRepo)
    {
        _writeRepoSlot = writeRepoSlot;
        _writeRepoSlotProblem = writeRepoSlotProblem;
        _readRepoSlot = readRepoSlot;
        _readRepoProblem = readRepoProblem;
        _uow = uow;
        _classSlotRepo = classSlotRepo;
    }

    public async Task<Guid> Handle(CreateClassSlotCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var mode = request.Mode?.ToLowerInvariant() ?? "problemset";
        if (mode is not ("problemset" or "contest"))
            throw new ArgumentException("Mode must be 'problemset' or 'contest'.");

        if (!await _classSlotRepo.ClassSemesterExistsAsync(request.ClassSemesterId, ct))
            throw new KeyNotFoundException("Class semester not found.");

        var slotExists = await _readRepoSlot.AnyAsync(
            new ClassSlotByClassSemesterAndSlotNoSpec(request.ClassSemesterId, request.SlotNo), ct);
        if (slotExists)
            throw new InvalidOperationException($"SlotNo {request.SlotNo} already exists in this class instance.");

        if (request.Problems is { Count: > 0 })
        {
            var (ok, error) = ValidateSlotPoints(request.Problems.Select(p => p.Points));
            if (!ok)
                throw new ArgumentException(error);
        }

        var slot = new ClassSlot
        {
            ClassSemesterId = request.ClassSemesterId,
            SlotNo = request.SlotNo,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Rules = request.Rules?.Trim(),
            OpenAt = request.OpenAt?.ToUniversalTime(),
            DueAt = request.DueAt?.ToUniversalTime(),
            CloseAt = request.CloseAt?.ToUniversalTime(),
            Mode = mode,
            IsPublished = false
        };

        await _writeRepoSlot.AddAsync(slot, ct);
        await _uow.SaveChangesAsync(ct);

        if (request.Problems is { Count: > 0 })
        {
            foreach (var p in request.Problems)
            {
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
            }
            await _uow.SaveChangesAsync(ct);
        }

        return slot.Id;
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
            return (false, $"Total points of problems in slot must not exceed {MaxSlotPoints}. Current total: {total}.");

        return (true, null);
    }
}
