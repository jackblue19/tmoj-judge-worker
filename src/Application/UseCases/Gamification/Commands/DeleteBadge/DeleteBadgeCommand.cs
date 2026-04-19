using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Commands.DeleteBadge
{
    public class DeleteBadgeCommand : IRequest<bool>
    {
        public Guid BadgeId { get; set; }
    }
}
