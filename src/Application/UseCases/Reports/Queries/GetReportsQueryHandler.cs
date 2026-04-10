using Application.UseCases.Reports.Dtos;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;

namespace Application.UseCases.Reports.Queries;

public class GetReportsQueryHandler
    : IRequestHandler<GetReportsQuery, List<ReportDto>>
{
    private readonly IReadRepository<ContentReport, Guid> _repo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IReadRepository<User, Guid> _userRepo;

    public GetReportsQueryHandler(
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

    public async Task<List<ReportDto>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var reports = await _repo.ListAsync(
            new AllReportsSpec(request.Status), ct);

        var result = new List<ReportDto>();

        foreach (var r in reports)
        {
            Guid? authorId = null;

            // ===== COMMENT =====
            if (r.TargetType == "comment")
            {
                var comment = await _commentRepo.GetByIdAsync(r.TargetId, ct);
                authorId = comment?.UserId;
            }
            // ===== DISCUSSION =====
            else if (r.TargetType == "discussion")
            {
                var discussion = await _discussionRepo.GetByIdAsync(r.TargetId, ct);
                authorId = discussion?.UserId;
            }

            string? authorName = null;

            if (authorId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(authorId.Value, ct);
                authorName = user?.DisplayName ?? user?.Username;
            }

            result.Add(new ReportDto
            {
                Id = r.Id,
                TargetId = r.TargetId,
                TargetType = r.TargetType,
                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,

                // NEW
                AuthorId = authorId,
                AuthorName = authorName
            });
        }

        return result;
    }
}