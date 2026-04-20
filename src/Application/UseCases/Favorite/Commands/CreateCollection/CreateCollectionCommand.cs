
using Application.UseCases.Favorite.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Commands.CreateCollection;

public class CreateCollectionCommand : IRequest<CreateCollectionResponseDto>
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public string Type { get; set; } = default!; // problem / contest / custom
    public bool IsVisibility { get; set; } = true; // public/private
}