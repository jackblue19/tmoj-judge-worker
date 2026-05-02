using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassContestProblemById;

public record GetClassContestProblemByIdQuery(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid ContestProblemId
) : IRequest<ContestProblemDto>;
