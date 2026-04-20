using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.UseCases.Favorite.Dtos;
using MediatR;

namespace Application.UseCases.Favorite.Commands.UpdateCollection;

public class UpdateCollectionCommand : IRequest<UpdateCollectionResponseDto>
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsVisibility { get; set; }
}