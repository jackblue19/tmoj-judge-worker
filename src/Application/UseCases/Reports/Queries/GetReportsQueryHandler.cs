using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Queries;

public class GetReportsQueryHandler
    : IRequestHandler<GetReportsQuery, List<ReportDto>>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly IDiscussionCommentRepository _commentRepo;
    private readonly IProblemDiscussionRepository _discussionRepo;
    private readonly IReadRepository<User, Guid> _userRepo;

    public GetReportsQueryHandler(
        IReadRepository<ContentReport, Guid> repo,
        IDiscussionCommentRepository commentRepo,
        IProblemDiscussionRepository discussionRepo,
        IReadRepository<User, Guid> userRepo)
    {
        _repo = repo;
        _commentRepo = commentRepo;
        _discussionRepo = discussionRepo;
        _userRepo = userRepo;
    }

    public async Task<List<ReportDto>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var reports = await _repo.ListAsync(
            new AllReportsSpec(request.Status), ct);

        var result = new List<ReportDto>();

        foreach (var r in reports)
        {
            Guid? authorId = null;
            string? authorName = null;
            string? contentPreview = null;
            Guid? problemId = null;
            string? redirectUrl = null;

            // =========================
            // COMMENT
            // =========================
            if (r.TargetType == "comment")
            {
                var comment = await _commentRepo.GetByIdWithDiscussionAsync(r.TargetId);

                if (comment != null)
                {
                    authorId = comment.UserId;
                    contentPreview = comment.Content;
                    problemId = comment.Discussion?.ProblemId;

                    if (problemId != null)
                    {
                        redirectUrl = $"/problems/{problemId}/discussion?commentId={comment.Id}";
                    }
                }
            }
            // =========================
            // DISCUSSION
            // =========================
            else if (r.TargetType == "discussion")
            {
                var discussion = await _discussionRepo.GetEntityByIdAsync(r.TargetId);

                if (discussion != null)
                {
                    authorId = discussion.UserId;
                    contentPreview = discussion.Content;
                    problemId = discussion.ProblemId;

                    redirectUrl = $"/problems/{problemId}/discussion/{discussion.Id}";
                }
            }

            // =========================
            // USER
            // =========================
            if (authorId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(authorId.Value, ct);
                authorName = user?.DisplayName ?? user?.Username;
            }

            // truncate
            if (!string.IsNullOrEmpty(contentPreview) && contentPreview.Length > 100)
            {
                contentPreview = contentPreview.Substring(0, 100) + "...";
            }

            result.Add(new ReportDto
            {
                Id = r.Id,
                TargetId = r.TargetId,
                TargetType = r.TargetType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,

                AuthorId = authorId,
                AuthorName = authorName,

                ContentPreview = contentPreview,
                ProblemId = problemId,
                RedirectUrl = redirectUrl
            });
        }

        return result;
    }
}