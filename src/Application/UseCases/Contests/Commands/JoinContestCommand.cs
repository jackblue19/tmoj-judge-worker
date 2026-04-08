using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class JoinContestCommand : IRequest<Guid>
{
    public Guid ContestId { get; set; }

    // optional (nếu team mode)
    public Guid? TeamId { get; set; }
}