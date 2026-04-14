using Application.UseCases.Editorials.Dtos;

using MediatR;

namespace Application.UseCases.ProblemEditorials.Commands;

public class UpdateProblemEditorialCommand : IRequest<ProblemEditorialDto>
{
    public Guid Id { get; set; }
    public string Content { get; set; } = null!;
}