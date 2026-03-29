using Application.UseCases.Testsets.Dtos;
using MediatR;

namespace Application.UseCases.Testsets.Queries;

public sealed record DownloadTestsetZipQuery(
    Guid ProblemId ,
    Guid TestsetId
) : IRequest<DownloadTestsetZipDto>;