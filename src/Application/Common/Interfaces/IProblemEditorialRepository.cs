using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IProblemEditorialRepository
{
    Task<ProblemEditorial?> GetByIdAsync(Guid id);

    Task<List<ProblemEditorial>> GetByProblemIdAsync(Guid problemId, int take);

    Task<Guid> CreateAsync(ProblemEditorial entity);

    void Update(ProblemEditorial entity);

    void Delete(ProblemEditorial entity);

    Task SaveChangesAsync();
}