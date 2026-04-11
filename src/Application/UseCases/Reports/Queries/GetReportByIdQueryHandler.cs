using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportDto>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IReadRepository<User, Guid> _userRepo;

    public GetReportByIdQueryHandler(
        IReadRepository<ContentReport, Guid> repo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
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

        if (report.TargetType == "comment")
        {
            var comment = await _commentRepo.GetByIdAsync(report.TargetId, ct);
            authorId = comment?.UserId;
        }
        else if (report.TargetType == "discussion")
        {
            var discussion = await _discussionRepo.GetByIdAsync(report.TargetId, ct);
            authorId = discussion?.UserId;
        }

        string authorName = "Unknown";

        if (authorId.HasValue)
        {
            var user = await _userRepo.GetByIdAsync(authorId.Value, ct);
            if (user != null)
            {
                authorName =
                    !string.IsNullOrEmpty(user.DisplayName)
                        ? user.DisplayName
                        : user.Username ?? "Unknown";
            }
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
            AuthorName = authorName
        };
    }
}