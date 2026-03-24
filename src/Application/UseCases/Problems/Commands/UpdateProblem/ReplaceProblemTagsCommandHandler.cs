using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed class ReplaceProblemTagsCommandHandler : IRequestHandler<ReplaceProblemTagsCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReplaceProblemTagsCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        ITagRepository tagRepository ,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemDetailDto> Handle(ReplaceProblemTagsCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        var entity = await _problemRepository.GetProblemForManagementAsync(
            request.ProblemId ,
            currentUserId ,
            isAdmin ,
            ct);

        if ( entity is null )
            throw new KeyNotFoundException("Problem not found or access denied.");

        var incomingIds = request.TagIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray() ?? [];

        var tags = await _tagRepository.GetTrackedByIdsAsync(incomingIds , ct);

        if ( incomingIds.Length != tags.Count )
            throw new InvalidOperationException("One or more tag ids do not exist.");

        entity.Tags.Clear();

        foreach ( var tag in tags )
        {
            entity.Tags.Add(tag);
        }

        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserId;

        await _unitOfWork.SaveChangesAsync(ct);

        var detail = await _problemRepository.GetProblemDetailForManagementAsync(
            entity.Id ,
            currentUserId ,
            isAdmin ,
            ct);

        return detail ?? throw new KeyNotFoundException("Problem detail not found after replace.");
    }
}