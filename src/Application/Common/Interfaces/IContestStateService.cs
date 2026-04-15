using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    namespace Application.Common.Interfaces
    {
    public enum ContestState
    {
        Upcoming,
        Running,
        Frozen,
        Ended
    }

    public interface IContestStateService
    {
        ContestState GetState(Contest contest, DateTime now);
    }
}
