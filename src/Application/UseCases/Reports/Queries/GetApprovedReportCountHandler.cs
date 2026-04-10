using Application.Common.Interfaces;
using Application.UseCases.Reports.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.UseCases.Reports.Queries
{
    public class GetApprovedReportCountHandler
      : IRequestHandler<GetApprovedReportCountQuery, int>
    {
        private readonly IReadRepository<ContentReport, Guid> _readRepo;

        public GetApprovedReportCountHandler(
            IReadRepository<ContentReport, Guid> readRepo)
        {
            _readRepo = readRepo;
        }

        public async Task<int> Handle(
            GetApprovedReportCountQuery request,
            CancellationToken ct)
        {
            var spec = new ApprovedReportCountSpec(
                request.TargetId,
                request.TargetType);

            var count = await _readRepo.CountAsync(spec, ct);

            return count;
        }
    }
}
