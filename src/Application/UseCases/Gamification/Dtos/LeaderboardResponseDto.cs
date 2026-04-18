using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Dtos
{
    public class LeaderboardResponseDto
    {
        public string Type { get; set; } = default!;
        public int Total { get; set; }
        public int Top { get; set; }

        public List<LeaderboardItemDto> Items { get; set; } = new();

        public LeaderboardMeDto? Me { get; set; }
    }

    public class LeaderboardMeDto
    {
        public int Rank { get; set; }
        public int Value { get; set; }
    }
}
