using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

public class FavoriteProblemDto
{
    public Guid ProblemId { get; set; }
    public string Title { get; set; } = default!;
    public string? Difficulty { get; set; }
    public string? TypeCode { get; set; }
    public string? StatusCode { get; set; }

    public bool IsFavorited { get; set; }
}
