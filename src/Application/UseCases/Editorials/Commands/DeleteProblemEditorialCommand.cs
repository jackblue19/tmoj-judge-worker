using MediatR;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.ProblemEditorials.Commands;

public class DeleteProblemEditorialCommand : IRequest
{
    public Guid Id { get; set; }
}