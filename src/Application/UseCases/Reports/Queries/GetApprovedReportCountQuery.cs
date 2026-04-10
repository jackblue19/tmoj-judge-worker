using Application.UseCases.Reports.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Reports.Queries
{
    public record GetApprovedReportCountQuery(
     Guid TargetId,
     string TargetType
 ) : IRequest<int>;
}
