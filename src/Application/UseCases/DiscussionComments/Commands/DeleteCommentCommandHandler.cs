using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.DiscussionComments.Commands
{
    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
    {
        private readonly IWriteRepository<DiscussionComment, Guid> _writeRepo;
        private readonly IReadRepository<DiscussionComment, Guid> _readRepo;
        private readonly IProblemDiscussionRepository _discussionRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _uow;

        public DeleteCommentCommandHandler(
            IWriteRepository<DiscussionComment, Guid> writeRepo,
            IReadRepository<DiscussionComment, Guid> readRepo,
            IProblemDiscussionRepository discussionRepo,
            ICurrentUserService currentUser,
            IUnitOfWork uow)
        {
            _writeRepo = writeRepo;
            _readRepo = readRepo;
            _discussionRepo = discussionRepo;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken ct)
        {
            var userId = _currentUser.UserId;
            if (userId is null)
                throw new UnauthorizedAccessException();

            var comment = await _readRepo.GetByIdAsync(request.CommentId, ct);
            if (comment == null)
                throw new Exception("Comment not found");

            var isCommentOwner = comment.UserId == userId.Value;
            var discussion = await _discussionRepo.GetByIdAsync(comment.DiscussionId);
            var isDiscussionOwner = discussion?.UserId == userId.Value;
            var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

            if (!isCommentOwner && !isDiscussionOwner && !isAdmin)
                throw new UnauthorizedAccessException("You are not allowed to delete this comment");

            _writeRepo.Remove(comment);
            await _uow.SaveChangesAsync(ct);

            return true;
        }
    }
}