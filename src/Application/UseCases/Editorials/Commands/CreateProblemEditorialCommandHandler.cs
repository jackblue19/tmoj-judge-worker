using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Npgsql;

namespace Application.UseCases.Editorials.Commands;

public class CreateProblemEditorialCommandHandler
    : IRequestHandler<CreateProblemEditorialCommand, Guid>
{
    private readonly IProblemEditorialRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CreateProblemEditorialCommandHandler(
        IProblemEditorialRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateProblemEditorialCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not logged in");

        var entity = new ProblemEditorial
        {
            Id = Guid.NewGuid(),
            ProblemId = request.ProblemId,
            AuthorId = userId,
            Content = request.Content,
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        try
        {
            return await _repo.CreateAsync(entity);
        }
        catch (Exception ex)
        {
            // 🔥 BẮT UNIQUE CONSTRAINT
            if (ex.InnerException is PostgresException pgEx &&
                pgEx.SqlState == "23505")
            {
                throw new Exception("EDITORIAL_ALREADY_EXISTS");
            }

            throw;
        }
    }
}