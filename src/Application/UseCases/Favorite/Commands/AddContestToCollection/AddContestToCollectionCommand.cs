using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Commands.AddContestToCollection;

public class AddContestToCollectionCommand
    : IRequest<AddContestToCollectionResult>
{
    public Guid UserId { get; set; }
    public Guid CollectionId { get; set; }
    public Guid ContestId { get; set; }
}