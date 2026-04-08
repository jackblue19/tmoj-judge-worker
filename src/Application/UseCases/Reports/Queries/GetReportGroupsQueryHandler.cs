using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.Reports.Dtos;

namespace Application.UseCases.Reports.Queries
{
    public class GetReportGroupsQueryHandler
     : IRequestHandler<GetReportGroupsQuery, List<ReportGroupDto>>
    {
        private readonly IContentReportRepository _repo;

        public GetReportGroupsQueryHandler(IContentReportRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ReportGroupDto>> Handle(
            GetReportGroupsQuery request,
            CancellationToken ct)
        {
            return await _repo.GetReportGroupsAsync(request.Status);
        }
    }
}
