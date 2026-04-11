using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Reports.Queries;

public class GetMyReportsQueryHandler
    : IRequestHandler<GetMyReportsQuery, List<ReportDto>>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly ICurrentUserService _currentUser;

    public GetMyReportsQueryHandler(
        IReadRepository<ContentReport, Guid> repo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        IReadRepository<User, Guid> userRepo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _commentRepo = commentRepo;
        _discussionRepo = discussionRepo;
        _userRepo = userRepo;
        _currentUser = currentUser;
    }

    public async Task<List<ReportDto>> Handle(GetMyReportsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var reports = await _repo.ListAsync(
            new ReportsByUserSpec(userId), ct);

        var result = new List<ReportDto>();

        foreach (var r in reports)
        {
            Guid? authorId = null;

            if (r.TargetType == "comment")
            {
                var comment = await _commentRepo.GetByIdAsync(r.TargetId, ct);
                authorId = comment?.UserId;
            }
            else if (r.TargetType == "discussion")
            {
                var discussion = await _discussionRepo.GetByIdAsync(r.TargetId, ct);
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

            result.Add(new ReportDto
            {
                Id = r.Id,
                TargetId = r.TargetId,
                TargetType = r.TargetType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                AuthorId = authorId,
                AuthorName = authorName
            });
        }

        return result;
    }
}