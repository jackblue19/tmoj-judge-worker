using Application.UseCases.Semesters.Dtos;
using MediatR;

namespace Application.UseCases.Semesters.Queries.GetSemesterById;

public record GetSemesterByIdQuery(Guid SemesterId) : IRequest<SemesterDto>;
