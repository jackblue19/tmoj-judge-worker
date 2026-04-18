using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Dtos
{
    public class LeaderboardItemDto
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = default!;
        public string? AvatarUrl { get; set; }

        public int Value { get; set; } // exp / streak / badge count
        public int Rank { get; set; }
    }
}
