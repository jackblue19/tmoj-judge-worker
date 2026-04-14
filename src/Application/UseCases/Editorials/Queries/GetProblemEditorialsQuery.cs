using Application.Common.Interfaces;
using Application.UseCases.Editorials.Dtos;
using MediatR;

namespace Application.UseCases.ProblemEditorials.Queries;

public class GetProblemEditorialsQuery : IRequest<List<ProblemEditorialDto>>
{
    public Guid ProblemId { get; set; }
    public int PageSize { get; set; } = 10;
   
}
