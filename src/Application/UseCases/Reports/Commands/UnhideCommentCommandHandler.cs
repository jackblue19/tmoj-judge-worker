using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Commands;

public class UnhideCommentCommandHandler : IRequestHandler<UnhideCommentCommand, Unit>
{
    private readonly IReadRepository<DiscussionComment, Guid> _readRepo;
    private readonly IWriteRepository<DiscussionComment, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public UnhideCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> readRepo,
        IWriteRepository<DiscussionComment, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task<Unit> Handle(UnhideCommentCommand request, CancellationToken ct)
    {
        var comment = await _readRepo.GetByIdAsync(request.CommentId, ct)
            ?? throw new Exception("Comment not found");

        comment.IsHidden = request.IsHidden;

        _writeRepo.Update(comment);
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}