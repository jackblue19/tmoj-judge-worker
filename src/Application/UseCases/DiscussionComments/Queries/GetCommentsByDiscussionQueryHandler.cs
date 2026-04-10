using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Dtos;
using MediatR;

namespace Application.UseCases.DiscussionComments.Queries;

public class GetCommentsByDiscussionQueryHandler
    : IRequestHandler<GetCommentsByDiscussionQuery, List<CommentResponseDto>>
{
    private readonly IDiscussionCommentRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetCommentsByDiscussionQueryHandler(
        IDiscussionCommentRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<CommentResponseDto>> Handle(
        GetCommentsByDiscussionQuery request,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        var comments = await _repo.GetByDiscussionIdAsync(request.DiscussionId);

        // 🔥 xử lý visibility (KHÔNG xoá node)
        foreach (var c in comments)
        {
            HandleVisibility(c, userId);
        }

        return comments;
    }

    // ===============================
    // 🔥 RECURSIVE HANDLE VISIBILITY
    // ===============================
    private void HandleVisibility(CommentResponseDto c, Guid? userId)
    {
        bool canSee =
            !c.IsHidden
            || (userId != null && c.UserId == userId)
            || _currentUser.IsInRole("admin")
            || _currentUser.IsInRole("manager");

        if (!canSee)
        {
            // 🔥 chỉ ẩn content, KHÔNG xoá node
            c.Content = "[This comment has been hidden]";
        }

        // 🔥 giữ nguyên replies
        foreach (var child in c.Replies)
        {
            HandleVisibility(child, userId);
        }
    }
}