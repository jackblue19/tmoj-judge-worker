using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Dtos
{
    public class LeaderboardRawDto
    {
        public Guid UserId { get; set; }
        public int Value { get; set; }
    }
}
