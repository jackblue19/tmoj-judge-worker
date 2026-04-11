using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportDto>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly IDiscussionCommentRepository _commentRepo;
    private readonly IProblemDiscussionRepository _discussionRepo;
    private readonly IReadRepository<User, Guid> _userRepo;

    public GetReportByIdQueryHandler(
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

    public async Task<ReportDto> Handle(GetReportByIdQuery request, CancellationToken ct)
    {
        var report = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new Exception("Report not found");

        Guid? authorId = null;
        string? authorName = null;
        string? contentPreview = null;
        Guid? problemId = null;
        string? redirectUrl = null;

        if (report.TargetType == "comment")
        {
            var comment = await _commentRepo.GetByIdWithDiscussionAsync(report.TargetId);

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
        else if (report.TargetType == "discussion")
        {
            var discussion = await _discussionRepo.GetEntityByIdAsync(report.TargetId);

            if (discussion != null)
            {
                authorId = discussion.UserId;
                contentPreview = discussion.Content;
                problemId = discussion.ProblemId;

                redirectUrl = $"/problems/{problemId}/discussion/{discussion.Id}";
            }
        }

        if (authorId.HasValue)
        {
            var user = await _userRepo.GetByIdAsync(authorId.Value, ct);
            authorName = user?.DisplayName ?? user?.Username;
        }

        return new ReportDto
        {
            Id = report.Id,
            TargetId = report.TargetId,
            TargetType = report.TargetType,
            Reason = report.Reason,
            Status = report.Status,
            CreatedAt = report.CreatedAt,

            AuthorId = authorId,
            AuthorName = authorName,

            ContentPreview = contentPreview,
            ProblemId = problemId,
            RedirectUrl = redirectUrl
        };
    }
}