using Application.UseCases.Testsets.Dtos;
using MediatR;

namespace Application.UseCases.Testsets.Queries;

public sealed record GetSampleTestcasesQuery(
    Guid ProblemId ,
    Guid TestsetId
) : IRequest<SampleTestcaseListDto>;