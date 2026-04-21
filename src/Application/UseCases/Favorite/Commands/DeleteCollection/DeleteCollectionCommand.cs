using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Commands.DeleteCollection;

public class DeleteCollectionCommand : IRequest<DeleteCollectionResponseDto>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
}