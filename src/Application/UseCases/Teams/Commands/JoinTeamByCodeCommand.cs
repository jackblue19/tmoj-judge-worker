using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Teams.Commands
{
    public class JoinTeamByCodeCommand : IRequest
    {
        public string Code { get; set; } = null!;
    }
}
