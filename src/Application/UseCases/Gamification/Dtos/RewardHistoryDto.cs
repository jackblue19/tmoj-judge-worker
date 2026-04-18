using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Dtos;

public class RewardHistoryDto
{
    public string Type { get; set; } = default!; // badge | coin | exp
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public string? Icon { get; set; }
    public decimal? Amount { get; set; } // coin / exp
    public DateTime CreatedAt { get; set; }
}