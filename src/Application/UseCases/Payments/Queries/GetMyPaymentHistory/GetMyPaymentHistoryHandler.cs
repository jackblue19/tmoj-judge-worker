using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Queries.GetMyPaymentHistory
{
    public class GetMyPaymentHistoryHandler : IRequestHandler<GetMyPaymentHistoryQuery, GetMyPaymentHistoryResult>
    {
        private readonly IPaymentRepository _repo;

        public GetMyPaymentHistoryHandler(IPaymentRepository repo)
        {
            _repo = repo;
        }

        public async Task<GetMyPaymentHistoryResult> Handle(GetMyPaymentHistoryQuery request, CancellationToken cancellationToken)
        {
            var result = await _repo.GetMyPaymentHistoryAsync(request.UserId, request.Page, request.PageSize);

            var totalPages = (int)Math.Ceiling(result.TotalItems / (double)request.PageSize);

            var itemsDto = result.Items.Select(x => new GetPaymentDto
            {
                PaymentId = x.PaymentId,
                UserId = x.UserId,
                Status = x.Status,
                Amount = x.AmountMoney,
                PaymentMethod = x.PaymentMethod,
                CreatedAt = x.CreatedAt,
                PaidAt = x.PaidAt
            }).ToList();

            return new GetMyPaymentHistoryResult
            {
                Items = itemsDto,
                TotalItems = result.TotalItems,
                TotalPages = totalPages,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
