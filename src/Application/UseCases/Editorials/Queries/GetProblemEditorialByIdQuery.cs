using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.ProblemEditorials.Queries;

public class GetProblemEditorialByIdQuery : IRequest<ProblemEditorialDto>
{
    public Guid Id { get; set; }
}
