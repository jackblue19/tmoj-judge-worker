using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.ProblemEditorials.Commands;

public class DeleteProblemEditorialCommandHandler
    : IRequestHandler<DeleteProblemEditorialCommand>
{
    private readonly IProblemEditorialRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public DeleteProblemEditorialCommandHandler(
        IProblemEditorialRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteProblemEditorialCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id);

        if (entity == null)
            throw new Exception($"Editorial {request.Id} not found");

        // 🔥 CHECK quyền (rất nên có)
        var userId = _currentUser.UserId;
        if (entity.AuthorId != userId)
            throw new UnauthorizedAccessException("You are not owner");

        _repo.Delete(entity);
        await _repo.SaveChangesAsync();
    }
}