using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassContestById;

public record GetClassContestByIdQuery(Guid ClassSemesterId, Guid ContestId, Guid UserId) : IRequest<ClassContestDto>;
