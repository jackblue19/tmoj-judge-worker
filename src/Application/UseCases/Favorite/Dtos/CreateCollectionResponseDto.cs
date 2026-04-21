using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

public class CreateCollectionResponseDto
{
    public bool IsSuccess { get; set; }

    public Guid? CollectionId { get; set; }

    public string? Name { get; set; }
    public string? Type { get; set; }

    public bool? IsVisibility { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}