using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassContests;

public record GetClassContestsQuery(Guid ClassSemesterId) : IRequest<List<ClassContestSummaryDto>>;
