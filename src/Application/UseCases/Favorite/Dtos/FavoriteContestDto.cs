using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

public class FavoriteContestDto
{
    public Guid ContestId { get; set; }

    public string Title { get; set; } = default!;
    public string? Slug { get; set; }
    public string? Description { get; set; }

    public string VisibilityCode { get; set; } = default!;
    public string? ContestType { get; set; }

    public bool AllowTeams { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public string Status { get; set; } = default!;
    public string ScoreboardMode { get; set; } = default!;

    public bool IsVirtual { get; set; }

    public bool IsFavorited { get; set; }
}