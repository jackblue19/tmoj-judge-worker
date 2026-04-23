using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Queries.GetAllPaymentHistory
{
    public class GetAllPaymentHistoryHandler : IRequestHandler<GetAllPaymentHistoryQuery, GetAllPaymentHistoryResult>
    {
        private readonly IPaymentRepository _repo;

        public GetAllPaymentHistoryHandler(IPaymentRepository repo)
        {
            _repo = repo;
        }

        public async Task<GetAllPaymentHistoryResult> Handle(GetAllPaymentHistoryQuery request, CancellationToken cancellationToken)
        {
            var result = await _repo.GetAllPaymentHistoryAsync(request.Page, request.PageSize);

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

            return new GetAllPaymentHistoryResult
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
