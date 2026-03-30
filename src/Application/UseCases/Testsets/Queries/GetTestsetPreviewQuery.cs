using Application.UseCases.Testsets.Dtos;
using MediatR;

namespace Application.UseCases.Testsets.Queries;

public sealed record GetTestsetPreviewQuery(
    Guid ProblemId ,
    Guid TestsetId
) : IRequest<TestcaseContentListDto>;