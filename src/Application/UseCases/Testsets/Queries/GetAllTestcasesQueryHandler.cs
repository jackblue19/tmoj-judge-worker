using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Testsets.Dtos;
using Application.UseCases.Testsets.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Testsets.Queries;

public sealed class GetAllTestcasesQueryHandler
    : IRequestHandler<GetAllTestcasesQuery , TestcaseContentListDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemRepo;
    private readonly IReadRepository<Testset , Guid> _testsetRepo;
    private readonly IReadRepository<Testcase , Guid> _testcaseRepo;
    private readonly IR2Service _r2Service;

    public GetAllTestcasesQueryHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> problemRepo ,
        IReadRepository<Testset , Guid> testsetRepo ,
        IReadRepository<Testcase , Guid> testcaseRepo ,
        IR2Service r2Service)
    {
        _currentUser = currentUser;
        _problemRepo = problemRepo;
        _testsetRepo = testsetRepo;
        _testcaseRepo = testcaseRepo;
        _r2Service = r2Service;
    }

    public async Task<TestcaseContentListDto> Handle(GetAllTestcasesQuery request , CancellationToken ct)
    {
        EnsureAuthenticated();

        var problem = await _problemRepo.GetByIdAsync(request.ProblemId , ct)
            ?? throw new KeyNotFoundException("Problem not found.");

        EnsureCanManage(problem);

        var testset = await _testsetRepo.GetByIdAsync(request.TestsetId , ct);
        if ( testset is null || testset.ProblemId != request.ProblemId )
            throw new KeyNotFoundException("Testset not found.");

        var testcases = await _testcaseRepo.ListAsync(
            new TestcasesByTestsetSpec(request.TestsetId) , ct);

        var items = new List<TestcaseContentItemDto>(testcases.Count);

        foreach ( var testcase in testcases )
        {
            var inputKey = $"{request.TestsetId:D}/{testcase.Ordinal:D3}/input.inp";
            var outputKey = $"{request.TestsetId:D}/{testcase.Ordinal:D3}/output.out";

            var input = await _r2Service.GetObjectTextAsync("Testset" , inputKey , ct);
            var output = await _r2Service.GetObjectTextAsync("Testset" , outputKey , ct);

            items.Add(new TestcaseContentItemDto
            {
                Ordinal = testcase.Ordinal ,
                Input = input ,
                Output = output
            });
        }

        return new TestcaseContentListDto
        {
            ProblemId = request.ProblemId ,
            TestsetId = request.TestsetId ,
            Count = items.Count ,
            Items = items
        };
    }

    private void EnsureAuthenticated()
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");
    }

    private void EnsureCanManage(Problem problem)
    {
        if ( _currentUser.IsInRole("Admin") ||
             _currentUser.IsInRole("admin") ||
             _currentUser.IsInRole("teacher") ||
             _currentUser.IsInRole("manager") )
            return;

        if ( problem.CreatedBy != _currentUser.UserId )
            throw new UnauthorizedAccessException("You dont have permission for this action");
        //throw new KeyNotFoundException("Problem not found or access denied.");
    }
}