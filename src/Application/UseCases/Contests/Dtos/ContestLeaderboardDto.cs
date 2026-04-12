using Application.UseCases.Contests.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Contests.Dtos
{
    public class ContestLeaderboardDto
    {
        public Guid ContestId { get; set; }
        public List<TeamLeaderboardDto> Teams { get; set; } 
    }
}
