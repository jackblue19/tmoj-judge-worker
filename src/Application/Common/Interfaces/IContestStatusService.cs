using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IContestStatusService
{
    string GetStatus(DateTime startAt, DateTime endAt);
    string GetPhase(DateTime startAt, DateTime endAt);
    bool CanJoin(DateTime startAt, DateTime endAt);
}