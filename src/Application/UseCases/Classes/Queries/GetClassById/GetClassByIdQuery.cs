using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassById;

public record GetClassByIdQuery(Guid ClassId) : IRequest<ClassDto>;
