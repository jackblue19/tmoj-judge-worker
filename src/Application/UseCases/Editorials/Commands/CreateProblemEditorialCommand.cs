using MediatR;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.Editorials.Commands;

public class CreateProblemEditorialCommand : IRequest<Guid>
{
    public Guid ProblemId { get; set; }
    public string Content { get; set; } = null!;
}