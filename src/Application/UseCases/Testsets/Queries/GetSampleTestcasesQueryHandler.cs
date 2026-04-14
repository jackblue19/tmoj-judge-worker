using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Testsets.Dtos;
using Application.UseCases.Testsets.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Testsets.Queries;

public sealed class GetSampleTestcasesQueryHandler
    : IRequestHandler<GetSampleTestcasesQuery , SampleTestcaseListDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Testset , Guid> _testsetReadRepository;
    private readonly IReadRepository<Testcase , Guid> _testcaseReadRepository;
    private readonly IR2Service _r2Service;

    public GetSampleTestcasesQueryHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IReadRepository<Testset , Guid> testsetReadRepository ,
        IReadRepository<Testcase , Guid> testcaseReadRepository ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemReadRepository = problemReadRepository;
        _testsetReadRepository = testsetReadRepository;
        _testcaseReadRepository = testcaseReadRepository;
        _r2Service = r2Service;
    }

    public async Task<SampleTestcaseListDto> Handle(GetSampleTestcasesQuery request , CancellationToken ct)
    {
        EnsureAuthenticated();

        var problem = await _problemReadRepository.GetByIdAsync(request.ProblemId , ct);
        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        EnsureCanManageProblem(problem);

        var testset = await _testsetReadRepository.GetByIdAsync(request.TestsetId , ct);
        if ( testset is null || testset.ProblemId != request.ProblemId )
            throw new KeyNotFoundException("Testset not found.");

        var testcases = await _testcaseReadRepository.ListAsync(
            new FirstThreeTestcasesByTestsetSpec(request.TestsetId) , ct);

        if ( testcases.Count == 0 )
        {
            return new SampleTestcaseListDto
            {
                ProblemId = request.ProblemId ,
                TestsetId = request.TestsetId ,
                Count = 0 ,
                Items = []
            };
        }

        var semaphore = new SemaphoreSlim(3);

        try
        {
            var tasks = testcases.Select(async testcase =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    if ( string.IsNullOrWhiteSpace(testcase.Input) )
                        throw new InvalidOperationException($"Missing input object key for ordinal {testcase.Ordinal}.");

                    if ( string.IsNullOrWhiteSpace(testcase.ExpectedOutput) )
                        throw new InvalidOperationException($"Missing output object key for ordinal {testcase.Ordinal}.");

                    var inputTask = _r2Service.GetObjectTextAsync("Testset" , testcase.Input , ct);
                    var outputTask = _r2Service.GetObjectTextAsync("Testset" , testcase.ExpectedOutput , ct);

                    await Task.WhenAll(inputTask , outputTask);

                    return new SampleTestcaseItemDto
                    {
                        Ordinal = testcase.Ordinal ,
                        Input = await inputTask ,
                        Output = await outputTask
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var items = await Task.WhenAll(tasks);

            return new SampleTestcaseListDto
            {
                ProblemId = request.ProblemId ,
                TestsetId = request.TestsetId ,
                Count = items.Length ,
                Items = items.OrderBy(x => x.Ordinal).ToList()
            };
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    private void EnsureAuthenticated()
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");
    }

    private void EnsureCanManageProblem(Problem problem)
    {
        var isAdmin = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("admin");
        if ( isAdmin ) return;

        var currentUserId = _currentUser.UserId!.Value;

        if ( problem.CreatedBy != currentUserId )
            throw new KeyNotFoundException("Problem not found or access denied.");
    }
}