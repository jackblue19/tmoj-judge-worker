using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using Application.UseCases.ProblemEditorials.Commands;
using MediatR;

namespace Application.UseCases.Editorials.Commands;

public class UpdateProblemEditorialCommandHandler
    : IRequestHandler<UpdateProblemEditorialCommand, ProblemEditorialDto>
{
    private readonly IProblemEditorialRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public UpdateProblemEditorialCommandHandler(
        IProblemEditorialRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ProblemEditorialDto> Handle(
        UpdateProblemEditorialCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not logged in");

        var entity = await _repo.GetByIdAsync(request.Id);

        if (entity == null)
            throw new Exception("Editorial not found");

        if (entity.AuthorId != userId)
            throw new UnauthorizedAccessException("You are not owner");

        entity.Content = request.Content;

        // 🔥 FIX
        entity.UpdatedAt = DateTime.Now;

        _repo.Update(entity);
        await _repo.SaveChangesAsync();

        return new ProblemEditorialDto
        {
            Id = entity.Id,
            ProblemId = entity.ProblemId,
            AuthorId = entity.AuthorId,
            Content = entity.Content,

            // 🔥 FIX nullable
            CreatedAt = entity.CreatedAt ?? DateTime.Now,
            UpdatedAt = entity.UpdatedAt ?? DateTime.Now
        };
    }
}