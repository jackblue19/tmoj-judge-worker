using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Dtos
{
    public class GamificationDto
    {
        public int Exp { get; set; }
        public int Level { get; set; }
        public int NextLevelExp { get; set; }

        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        public decimal Coins { get; set; }
    }
}
