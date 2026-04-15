using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IContestFreezePolicy
    {
        bool IsFrozen(Contest contest);
        void EnsureViewAllowed(Contest contest);
        void EnsureSubmitAllowed(Contest contest);
    }
}
