using Application.UseCases.Testsets.Dtos;
using MediatR;

namespace Application.UseCases.Testsets.Queries;

public sealed record GetAllTestcasesQuery(
    Guid ProblemId ,
    Guid TestsetId
) : IRequest<TestcaseContentListDto>;