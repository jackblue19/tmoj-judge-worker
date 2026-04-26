namespace Application.Common.Interfaces;

public interface ISemesterRepository
{
    Task<bool> HasActiveClassesAsync(Guid semesterId, CancellationToken ct = default);
}
