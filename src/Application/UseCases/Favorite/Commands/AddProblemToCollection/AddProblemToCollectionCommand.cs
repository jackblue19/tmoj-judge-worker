using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Commands.AddProblemToCollection;

public class AddProblemToCollectionCommand
    : IRequest<AddProblemToCollectionResult>
{
    public Guid UserId { get; set; }
    public Guid CollectionId { get; set; }
    public Guid ProblemId { get; set; }
}