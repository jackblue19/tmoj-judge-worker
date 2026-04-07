using Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.ProblemDiscussions.Commands
{
    public class DeleteDiscussionCommandHandler : IRequestHandler<DeleteDiscussionCommand, bool>
    {
        private readonly IProblemDiscussionRepository _discussionRepo;
        private readonly ICurrentUserService _currentUser;

        public DeleteDiscussionCommandHandler(
            IProblemDiscussionRepository discussionRepo,
            ICurrentUserService currentUser)
        {
            _discussionRepo = discussionRepo;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(DeleteDiscussionCommand request, CancellationToken ct)
        {
            var discussion = await _discussionRepo.GetByIdAsync(request.Id);
            if (discussion == null)
                throw new Exception("Discussion not found");

            var userId = _currentUser.UserId;
            var isOwner = discussion.UserId == userId;
            var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

            if (!isOwner && !isAdmin)
                throw new UnauthorizedAccessException("You are not allowed to delete this discussion.");

            await _discussionRepo.DeleteDiscussionWithCommentsAsync(request.Id);
            return true;
        }
    }
}