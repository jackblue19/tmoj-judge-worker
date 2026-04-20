using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

public class CheckFavoriteResponseDto
{
    public bool IsFavorited { get; set; }

    // optional nhưng rất hữu ích debug/UI
    public string? Type { get; set; } // problem / contest
}
