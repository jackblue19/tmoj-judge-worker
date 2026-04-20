using MediatR;
using Application.UseCases.Favorite.Dtos;
namespace Application.UseCases.Favorite.Commands.CopyCollection;

public class CopyCollectionCommand : IRequest<CopyCollectionResult>
{
    public Guid UserId { get; set; }
    public Guid SourceCollectionId { get; set; }
}