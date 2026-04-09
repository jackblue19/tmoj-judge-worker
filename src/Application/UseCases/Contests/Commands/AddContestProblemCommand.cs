using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Contests.Commands
{
    public record AddContestProblemCommand(
     Guid ContestId,
     Guid ProblemId,
     string? Alias,
     int? Ordinal,
     int? DisplayIndex,
     int? Points,
     int? MaxScore,
     int? TimeLimitMs,
     int? MemoryLimitKb,
     int? OutputLimitKb,
     int? PenaltyPerWrong,
     string? ScoringCode,
     Guid? OverrideTestsetId
 ) : IRequest<Guid>;
}
