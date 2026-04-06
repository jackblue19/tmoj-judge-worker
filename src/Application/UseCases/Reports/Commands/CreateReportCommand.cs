using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Reports.Commands;

public record CreateReportCommand(
    Guid TargetId,
    string TargetType,
    string Reason
) : IRequest<Guid>;
