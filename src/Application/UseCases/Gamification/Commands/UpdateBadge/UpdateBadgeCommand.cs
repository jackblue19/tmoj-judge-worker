using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Commands.UpdateBadge
{
    public class UpdateBadgeCommand : IRequest<bool>
    {
        public Guid BadgeId { get; set; }

        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string BadgeCategory { get; set; } = default!;
        public int BadgeLevel { get; set; }
    }
}
