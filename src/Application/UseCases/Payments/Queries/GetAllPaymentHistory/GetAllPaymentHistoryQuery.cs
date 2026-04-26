using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Queries.GetAllPaymentHistory
{
    public class GetAllPaymentHistoryQuery : IRequest<GetAllPaymentHistoryResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAllPaymentHistoryResult
    {
        public List<GetPaymentDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
