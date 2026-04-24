namespace Application.Common.Interfaces;

public interface IClassSlotRepository
{
    Task<bool> ClassSemesterExistsAsync(Guid classSemesterId, CancellationToken ct = default);
    Task<bool> ProblemExistsAsync(Guid problemId, CancellationToken ct = default);
}
