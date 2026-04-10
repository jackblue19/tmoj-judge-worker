using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Application.UseCases.Reports.Commands;

public record ApproveReportCommand(
    Guid ReportId,
    string? Reason
) : IRequest<Unit>;