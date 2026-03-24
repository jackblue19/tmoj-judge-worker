using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed class GetProblemDetailQueryHandler : IRequestHandler<GetProblemDetailQuery , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;

    public GetProblemDetailQueryHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
    }

    public async Task<ProblemDetailDto> Handle(GetProblemDetailQuery request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        var detail = await _problemRepository.GetProblemDetailForManagementAsync(
            request.ProblemId ,
            currentUserId ,
            isAdmin ,
            ct);

        if ( detail is null )
            throw new KeyNotFoundException("Problem not found or access denied.");

        return detail;
    }
}
